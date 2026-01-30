using Domain.Enumi;
using Domain.Modeli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Servisi
{
    public interface IServerState1
    {
        //Stolovi - samo za citanje 
        IReadOnlyDictionary<int, Table> Tables { get; }

        //Aktivne porudzbine 
        IReadOnlyCollection<Order> ActiveOrders { get; }

        //Metode za rad sa porudzbinama 
        bool TryGetActiveOrder(int orderId, out Order order);
        void AddActiveOrder(Order order);
        void RemoveActiveOrder(int orderId);

        //Rad sa cekacujim porudzbinama
        void PushWaiting(OrderItem item, OrderItemType type);
        bool TryPopWaiting(OrderItemType type, out OrderItem item);

        List<Order> GetActiveOrdersForTable(int brojStola);
        void RemoveActiveOrdersForTable(int brojStola);

        int RegisterItem(OrderItem item);
        bool TryGetItem(int itemId, out OrderItem item);
        bool SetItemStatus(int itemId, OrderStatus newStatus);
        List<(int itemId, OrderItem item)> GetReadyItemsForTable(int brojStola);
        void ClearReadyItemsForTable(int brojStola);


         Table GetTable(int tableId);
        IReadOnlyList<Table> GetAllTables();
        void EnsureTableExists(int tableId);
        bool TryOccupyTable(int tableId, int guests);
        void FreeTable(int tableId);

        //Rezervacije 
        Reservation GetReservation(int tableId);
        void CleanupExpiredReservations(DateTime now);
        bool TryAddReservation(Reservation r);
    }
}
