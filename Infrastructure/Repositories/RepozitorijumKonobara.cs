using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Core.Repositories;

namespace Infrastructure.Repositories
{
    public class RepozitorijumKonobara : IRepozitorijumKonobara
    {
        private static readonly ConcurrentBag<int> konobari = new ConcurrentBag<int>();

        public void DodajKonobara(int id)
        {
            konobari.Add(id);
        }

        public List<int> DobaviKonobare()
        {
            return konobari.ToList();
        }

        public void ObrisiKonobara(int id)
        {
            //
        }
    }
}
