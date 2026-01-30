using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Servisi
{
    public static class PriceCatalog
    {
        private static readonly Dictionary<string, double> _prices = new Dictionary<string, double>(System.StringComparer.OrdinalIgnoreCase)
        {
            {"Kafa",180 },
            {"Sok", 220 },
            {"Pica",750 },
            {"Palacinka",350 }
        };
        public static bool TryGetPrice(string naziv, out double cena)
            => _prices.TryGetValue(naziv?.Trim() ?? "", out cena);
    }
}
