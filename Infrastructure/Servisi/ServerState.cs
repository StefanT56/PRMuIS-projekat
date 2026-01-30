using Application.Servisi;
using Domain.Enumi;
using Domain.Modeli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Servisi
{
    public class ServerState : IServerState1
    {
        private readonly object _lock = new object();

        // Kolekcija stolova
        private readonly Dictionary<int, Table> _tables = new Dictionary<int, Table>();

        // Rezervacije stolova
        private readonly Dictionary<int, Reservation> _reservations = new Dictionary<int, Reservation>();

        // Resursi (kuvar/barmen/konobar)
        private readonly Dictionary<int, ClientInfo> _resources = new Dictionary<int, ClientInfo>();

        // Aktivne porudzbine
        private readonly Dictionary<int, Order> _activeOrders = new Dictionary<int, Order>();

        // Spremne stavke po stolu
        private readonly Dictionary<int, List<int>> _readyByTable = new Dictionary<int, List<int>>();

        // Queue
        private readonly Queue<OrderItem> _foodQueue = new Queue<OrderItem>();
        private readonly Queue<OrderItem> _drinkQueue = new Queue<OrderItem>();

        // Stack čekanja
        private readonly Stack<OrderItem> _foodStack = new Stack<OrderItem>();
        private readonly Stack<OrderItem> _drinkStack = new Stack<OrderItem>();

        private int _nextItemId = 0;

        // itemId -> OrderItem
        private readonly Dictionary<int, OrderItem> _itemsById = new Dictionary<int, OrderItem>();

        public ServerState(IEnumerable<Table> tables)
        {
            foreach (var t in tables)
                _tables[t.Broj] = t;
        }

        // -----------------
        // Stolovi / stanje
        // -----------------

        public IReadOnlyDictionary<int, Table> Tables
        {
            get { lock (_lock) return new Dictionary<int, Table>(_tables); }
        }

        public Table GetTable(int tableId)
        {
            lock (_lock)
            {
                Table t;
                _tables.TryGetValue(tableId, out t);
                return t;
            }
        }

        public IReadOnlyList<Table> GetAllTables()
        {
            lock (_lock) return _tables.Values.ToList();
        }

        public void EnsureTableExists(int tableId)
        {
            lock (_lock)
            {
                if (!_tables.ContainsKey(tableId))
                    _tables[tableId] = new Table { Broj = tableId, Zauzetost = TableType.SLOBODAN };
            }
        }

        public bool TryOccupyTable(int tableId, int guests)
        {
            lock (_lock)
            {
                EnsureTableExists(tableId);

                var t = _tables[tableId];

                // Ako je rezervisan - ne daj da se zauzme
                if (t.Zauzetost == TableType.REZERVISAN)
                    return false;

                t.Zauzetost = TableType.ZAUZET;

                return true;
            }
        }

        public void FreeTable(int tableId)
        {
            lock (_lock)
            {
                if (!_tables.ContainsKey(tableId)) return;

                var t = _tables[tableId];
                t.Zauzetost = TableType.SLOBODAN;

                // očisti aktivne porudžbine + ready stavke
                RemoveActiveOrdersForTable(tableId);
                _readyByTable.Remove(tableId);
            }
        }

        // -----------------
        // Porudžbine
        // -----------------

        public IReadOnlyCollection<Order> ActiveOrders
        {
            get { lock (_lock) return _activeOrders.Values.ToList(); }
        }

        public bool TryGetActiveOrder(int orderId, out Order order)
        {
            lock (_lock)
                return _activeOrders.TryGetValue(orderId, out order);
        }

        public void AddActiveOrder(Order order)
        {
            lock (_lock)
                _activeOrders[order.id] = order;
        }

        public void RemoveActiveOrder(int orderId)
        {
            lock (_lock)
                _activeOrders.Remove(orderId);
        }

        public List<Order> GetActiveOrdersForTable(int brojStola)
        {
            lock (_lock)
                return _activeOrders.Values.Where(o => o.BrojStola == brojStola).ToList();
        }

        public void RemoveActiveOrdersForTable(int brojStola)
        {
            lock (_lock)
            {
                var ids = _activeOrders
                    .Where(kvp => kvp.Value.BrojStola == brojStola)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var id in ids)
                    _activeOrders.Remove(id);
            }
        }

        // -----------------
        // Waiting stack (order čekanje)
        // -----------------

        public void PushWaiting(OrderItem order, OrderItemType type)
        {
            lock (_lock)
            {
                if (type == OrderItemType.Hrana)
                    _foodStack.Push(order);
                else
                    _drinkStack.Push(order);
            }
        }

        public bool TryPopWaiting(OrderItemType type, out OrderItem order)
        {
            lock (_lock)
            {
                if (type == OrderItemType.Hrana && _foodStack.Count > 0)
                {
                    order = _foodStack.Pop();
                    return true;
                }

                if (type == OrderItemType.Pice && _drinkStack.Count > 0)
                {
                    order = _drinkStack.Pop();
                    return true;
                }

                order = null;
                return false;
            }
        }

        // -----------------
        // Stavke (OrderItem)
        // -----------------

        public int RegisterItem(OrderItem item)
        {
            lock (_lock)
            {
                int itemId = ++_nextItemId;
                _itemsById[itemId] = item;
                return itemId;
            }
        }

        public bool TryGetItem(int itemId, out OrderItem item)
        {
            lock (_lock)
                return _itemsById.TryGetValue(itemId, out item);
        }

        public bool SetItemStatus(int itemId, OrderStatus newStatus)
        {
            lock (_lock)
            {
                OrderItem item;
                if (!_itemsById.TryGetValue(itemId, out item))
                    return false;

                item.Status = newStatus;

                if (newStatus == OrderStatus.Spremno)
                {
                    List<int> list;
                    if (!_readyByTable.TryGetValue(item.BrojStola, out list))
                    {
                        list = new List<int>();
                        _readyByTable[item.BrojStola] = list;
                    }

                    if (!list.Contains(itemId))
                        list.Add(itemId);
                }

                return true;
            }
        }

        public List<(int itemId, OrderItem item)> GetReadyItemsForTable(int brojStola)
        {
            lock (_lock)
            {
                List<int> ids;
                if (!_readyByTable.TryGetValue(brojStola, out ids) || ids.Count == 0)
                    return new List<(int, OrderItem)>();

                var result = new List<(int, OrderItem)>();

                foreach (var id in ids)
                {
                    OrderItem item;
                    if (_itemsById.TryGetValue(id, out item))
                        result.Add((id, item));
                }

                return result;
            }
        }

        public void ClearReadyItemsForTable(int brojStola)
        {
            lock (_lock)
                _readyByTable.Remove(brojStola);
        }

        // -----------------
        // Rezervacije
        // -----------------

        public bool TryAddReservation(Reservation r)
        {
            if (r == null) return false;
            if (r.VrijemeKraj <= r.VrijemePocetak) return false;

            lock (_lock)
            {
                EnsureTableExists(r.BrojStola);

                // ne dozvoli preklapanje sa postojećom rezervacijom
                if (_reservations.TryGetValue(r.BrojStola, out var existing))
                {
                    // overlap: (start < existing.End) && (end > existing.Start)
                    if (r.VrijemePocetak < existing.VrijemeKraj && r.VrijemeKraj > existing.VrijemePocetak)
                        return false;
                }

                _reservations[r.BrojStola] = r;

                // sto ide u REZERVISAN ako trenutno nije zauzet
                var t = _tables[r.BrojStola];
                if (t.Zauzetost == TableType.SLOBODAN)
                    t.Zauzetost = TableType.REZERVISAN;

                return true;
            }
        }

        public void CleanupExpiredReservations(DateTime now)
        {
            lock (_lock)
            {
                var expired = _reservations
                    .Where(kv => kv.Value.IsExpired(now))
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var tableId in expired)
                {
                    _reservations.Remove(tableId);

                    if (_tables.TryGetValue(tableId, out var t) && t.Zauzetost == TableType.REZERVISAN)
                        t.Zauzetost = TableType.SLOBODAN;
                }
            }
        }

        public Reservation GetReservation(int tableId)
        {
            lock (_lock)
            {
                _reservations.TryGetValue(tableId, out var r);
                return r;
            }
        }

        // -----------------
        // Resursi (Kuvar/Barmen)
        // -----------------
        public void RegisterOrUpdateResource(ClientInfo r)
        {
            if (r == null || r.Id<=0) { return; }

            lock (_lock)
            {
                _resources[r.Id] = r;
            }
        }

        public IReadOnlyList<ClientInfo> GetResources()
        {
            lock (_lock) return _resources.Values.ToList();
        }

        public bool TryAcquireResource(ClientRole type, out ClientInfo resource)
        {
            lock (_lock)
            {
                resource = _resources.Values
                    .FirstOrDefault(x => x.role == type && x.zauzet == false);

                if (resource == null) return false;

                resource.zauzet = true;
                return true;
            }
        }

        public void ReleaseResource(int resourceId)
        {
            lock (_lock)
            {
                if (_resources.TryGetValue(resourceId, out var r))
                {
                    r.zauzet = false;
                }
            }
        }
        public void EnqueueItemForWork(OrderItem item)
        {
            if (item == null) return;

            lock (_lock)
            {
                bool isFood = item.Kategorija == OrderItemType.Hrana; 
                bool isDrink = item.Kategorija == OrderItemType.Pice;

                if (isFood)
                {
                    // ako nema slobodnog kuvara -> stack
                    if (_resources.Values.Any(r => r.role == ClientRole.Kuvar && !r.zauzet))
                        _foodQueue.Enqueue(item);
                    else
                        _foodStack.Push(item);
                }
                else if (isDrink)
                {
                    if (_resources.Values.Any(r => r.role == ClientRole.Barmen && !r.zauzet))
                        _drinkQueue.Enqueue(item);
                    else
                        _drinkStack.Push(item);
                }
            }
        }

        public bool TryDequeueNextFor(ClientRole type, out OrderItem item)
        {
            lock (_lock)
            {
                item = null;

                if (type == ClientRole.Kuvar)
                {
                    if (_foodStack.Count > 0) { item = _foodStack.Pop(); return true; }
                    if (_foodQueue.Count > 0) { item = _foodQueue.Dequeue(); return true; }
                    return false;
                }

                if (type == ClientRole.Barmen)
                {
                    if (_drinkStack.Count > 0) { item = _drinkStack.Pop(); return true; }
                    if (_drinkQueue.Count > 0) { item = _drinkQueue.Dequeue(); return true; }
                    return false;
                }

                return false;
            }
        }
    }
}
