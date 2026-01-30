using Domain.Enumi;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Parsing
{
    internal class OrderParser
    {
        // items format: Naziv:Kolicina,Naziv:Kolicina
        // npr "Kafa:2,Pica:1"
        public static List<(string name, int qty)> ParseItems(string items)
        {
            if (string.IsNullOrWhiteSpace(items)) return new List<(string, int)>();

            return items.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Select(part =>
                        {
                            var kv = part.Split(':');
                            if (kv.Length != 2) throw new FormatException("Neispravan format stavki. Očekujem Naziv:Kolicina.");

                            string name = kv[0].Trim();
                            if (!int.TryParse(kv[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int qty) || qty <= 0)
                                throw new FormatException("Količina mora biti ceo broj > 0.");

                            return (name, qty);
                        })
                        .ToList();
        }
        public static OrderItemType GuessType(string itemName)
        {
            string n = (itemName ?? "").ToLowerInvariant();
            if (n.Contains("kafa") || n.Contains("pivo") || n.Contains("sok") || n.Contains("voda") || n.Contains("rakija"))
                return OrderItemType.Pice;

            return OrderItemType.Hrana;
        }
    }
}
