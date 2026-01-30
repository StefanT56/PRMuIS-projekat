using Domain.Enumi;
using Domain.Modeli;
using Domain.Poruke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Servisi
{
    public class OrderDispatcher
    {
        private readonly IServerState1 _state1;

        private readonly Func<ClientRole, NetworkMessage, bool> _sendToRole;

        //stanje resursa 
        private ClientStatus _kuvar = ClientStatus.Slobodan;
        private ClientStatus _barmen = ClientStatus.Slobodan;

        private readonly object _lock = new object();

        public OrderDispatcher(IServerState1 state, Func<ClientRole, NetworkMessage, bool> sendToRole)
        {
            _state1 = state;
            _sendToRole = sendToRole;
        }

        public void OnNewOrder(Order order)
        {
            lock (_lock)
            {
                _state1.AddActiveOrder(order);

                if (order?.Stavke == null || order.Stavke.Count == 0)
                    return;

                foreach (var item in order.Stavke)
                {
                    if (item == null) continue;

                    var itemType = item.Kategorija; // Hrana / Pice (OrderItemType)

                    if (itemType == OrderItemType.Hrana)
                    {
                        if (_kuvar == ClientStatus.Slobodan)
                        {
                            _kuvar = ClientStatus.Zauzet;
                            SendTaskToWorker(ClientRole.Kuvar, item, itemType);
                        }
                        else
                        {
                            _state1.PushWaiting(item, itemType);
                        }
                    }
                    else // Pice
                    {
                        if (_barmen == ClientStatus.Slobodan)
                        {
                            _barmen = ClientStatus.Zauzet;
                            SendTaskToWorker(ClientRole.Barmen, item, itemType);
                        }
                        else
                        {
                            _state1.PushWaiting(item, itemType);
                        }
                    }
                }
            }
        }


        // Poziva server kad kuvar/barmen javi da je zavrsio zadatak
        public void OnTaskDone(TaskDoneRequest req)
        {
            lock (_lock)
            {
                if (req.Tip == OrderItemType.Hrana)
                    _kuvar = ClientStatus.Slobodan;
                else
                    _barmen = ClientStatus.Slobodan;

                // ako postoji cekajuca porudzbina iste vrste odmah dodeli
                if (_state1.TryPopWaiting(req.Tip, out var next) && next != null)
                {
                    if (req.Tip == OrderItemType.Hrana)
                        _kuvar = ClientStatus.Zauzet;
                    else
                        _barmen = ClientStatus.Zauzet;

                    SendTaskToWorker(req.Tip == OrderItemType.Hrana ? ClientRole.Kuvar : ClientRole.Barmen, next, req.Tip);
                }
                var readyPayload = new OrderReadyMessage
                {
                    OrderId = req.TaskId,
                    BrojStola = req.BrojStola
                };
                _sendToRole(ClientRole.Konobar, Wrap(MessageType.OrderReady, readyPayload));
            }
        }

        private OrderItemType ResolveOrderType(Order order)
        {
            if (order.Stavke != null && order.Stavke.Count > 0)
                return order.Stavke[0].Kategorija;

            return OrderItemType.Hrana;
        }

        private void SendTaskToWorker(ClientRole workerRole, OrderItem orderItem, OrderItemType type)
        {
            if (orderItem == null)
                return;

            // Ako imaš Kategorija na OrderItem, ovo čuva konzistentnost.
            // Ako nemaš, slobodno obriši ovaj if.
            if (orderItem.Kategorija != type)
                return;

            var payload = new TaskAssignedMessage
            {
                TaskId = orderItem.id,          // ili orderItem.ItemId / orderItem.TaskId -> stavi polje koje stvarno imaš
                BrojStola = orderItem.BrojStola, // ako OrderItem nema BrojStola, vidi napomenu ispod
                stavka = orderItem
            };

            _sendToRole(workerRole, Wrap(MessageType.TaskAssigned, payload));
        }


        private NetworkMessage Wrap(MessageType type, object data)
        {
            return new NetworkMessage
            {
                type = type,
                Data = data
            };
        }
    }
}
