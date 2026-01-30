using Application.Servisi;
using Domain.Enumi;
using Domain.Modeli;
using Infrastructure.Servisi;
using Server.Parsing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Handler
{
    public class WaiterMessageHandler : IMessageHandler
    {

        private readonly IServerState1 _state;
        private readonly IOrderNotifier _notifier;
        private static int _nextOrderId = 0;

        public WaiterMessageHandler(IServerState1 state, IOrderNotifier notifier)
        {
            _state = state;
            _notifier = notifier;
        }

        public string Handle(int clientId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "ERR|msg=Prazna poruka";

            if (message.Equals("PING", StringComparison.OrdinalIgnoreCase))
                return "OK|msg=PONG";

            if (message.StartsWith("ORDER_ADD|", StringComparison.OrdinalIgnoreCase))
                return HandleOrderAdd(clientId, message);

            if (message.StartsWith("BILL_GET|", StringComparison.OrdinalIgnoreCase))
                return HandleBillGet(message);

            if (message.StartsWith("BILL_PAY|", StringComparison.OrdinalIgnoreCase))
                return HandleBillPay(message);

            if (message.StartsWith("READY_GET|", StringComparison.OrdinalIgnoreCase))
            {
                string tableStr = GetValue(message, "table");
                if (!int.TryParse(tableStr, out int brojStola))
                    return "ERR|msg=Neispravan broj stola";

                var ready = _state.GetReadyItemsForTable(brojStola);
                if (ready.Count == 0)
                    return $"OK|msg=Nema spremnih stavki|sto={brojStola}";

                // format: itemId:Naziv;itemId:Naziv
                var grouped = ready
                .GroupBy(x => x.item.Naziv, StringComparer.OrdinalIgnoreCase)
                .Select(g => $"{g.Key}:{g.Count()}");

                string list = string.Join(",", grouped);

                return $"OK|msg=Spremne stavke|sto={brojStola}|items={list}";
            }


            return "ERR|msg=Nepoznata komanda";
        }

        private string HandleOrderAdd(int konobarId, string msg)
        {
            string tableStr = GetValue(msg, "table");
            string itemsStr = GetValue(msg, "items");

            if (!int.TryParse(tableStr, out int brojStola))
                return "ERR|msg=Neispravan broj stola";

            if (string.IsNullOrWhiteSpace(itemsStr))
                return "ERR|msg=Nedostaju stavke porudžbine";

            var order = new Order
            {
                id = Interlocked.Increment(ref _nextOrderId),
                BrojStola = brojStola,
                Stavke = new List<OrderItem>()
            };

            var parsedItems = OrderParser.ParseItems(itemsStr);

            foreach (var (naziv, kolicina) in parsedItems)
            {
                if (!PriceCatalog.TryGetPrice(naziv, out double cenaJed))
                    return $"ERR|msg=Nepoznata stavka u cenovniku: {naziv}";

                var tip = OrderParser.GuessType(naziv);

                for (int i = 0; i < kolicina; i++)
                {
                    var item = new OrderItem
                    {
                        Naziv = naziv,
                        Kategorija = tip,
                        cena = cenaJed,
                        Status = OrderStatus.Kreirana,
                        KonobarId = konobarId,
                        BrojStola = brojStola
                    };

                    int itemId = _state.RegisterItem(item);

                    order.Stavke.Add(item);

                    _notifier.NotifyNewItemAsync(itemId, order.id, item).GetAwaiter().GetResult();

                    _state.PushWaiting(item, tip);
                }
            }

            _state.AddActiveOrder(order);

            double total = order.Stavke.Sum(x => x.cena);
            return $"OK|msg=Porudžbina je prihvaćena|orderId={order.id}|sto={brojStola}|iznos={total}";
        }

        private string HandleBillGet(string msg)
        {
            string tableStr = GetValue(msg, "table");
            if (!int.TryParse(tableStr, out int brojStola))
                return "ERR|msg=Neispravan broj stola";

            var orders = _state.GetActiveOrdersForTable(brojStola);
            if (orders.Count == 0)
                return $"OK|msg=Nema aktivnih porudžbina|sto={brojStola}|iznos=0";

            double total = orders.Sum(o => o.Stavke.Sum(i => i.cena));

            // (opciono) kratka specifikacija stavki po nazivu
            var grouped = orders.SelectMany(o => o.Stavke)
                                .GroupBy(i => i.Naziv, StringComparer.OrdinalIgnoreCase)
                                .Select(g => $"{g.Key}:{g.Count()}x{g.First().cena}")
                                .ToArray();

            string items = string.Join(";", grouped);

            return $"OK|msg=Račun je izračunat|sto={brojStola}|iznos={total}|stavke={items}";
        }

        private string HandleBillPay(string msg)
        {
            string tableStr = GetValue(msg, "table");
            string paidStr = GetValue(msg, "paid");

            if (!int.TryParse(tableStr, out int brojStola))
                return "ERR|msg=Neispravan broj stola";

            if (!double.TryParse(paidStr, NumberStyles.Number, CultureInfo.InvariantCulture, out double paid))
                return "ERR|msg=Neispravan iznos uplate";

            var orders = _state.GetActiveOrdersForTable(brojStola);
            double total = orders.Sum(o => o.Stavke.Sum(i => i.cena));

            if (total <= 0)
                return $"ERR|msg=Nema šta da se plati|sto={brojStola}";

            if (paid < total)
                return $"ERR|msg=Nedovoljno novca|sto={brojStola}|iznos={total}|uplaceno={paid}|fali={total - paid}";

            double kusur = paid - total;

            // posle plaćanja sklanjamo aktivne porudžbine za taj sto
            _state.RemoveActiveOrdersForTable(brojStola);

            return $"OK|msg=Plaćeno|sto={brojStola}|iznos={total}|uplaceno={paid}|kusur={kusur}";
        }

        private static string GetValue(string msg, string key)
        {
            string needle = "|" + key + "=";
            int i = msg.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return null;

            i += needle.Length;
            int end = msg.IndexOf('|', i);
            if (end < 0) end = msg.Length;

            return msg.Substring(i, end - i);
        }
    }
}
