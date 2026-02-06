using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Core.Models;
using Core.Repositories;

namespace Infrastructure.Mreza
{
    public class DirektorijumKlijenata : IDirektorijumKlijenata
    {
        private readonly ConcurrentDictionary<int, InfoKlijenta> klijenti
            = new ConcurrentDictionary<int, InfoKlijenta>();

        public void Registruj(InfoKlijenta klijent)
            => klijenti[klijent.Id] = klijent;

        public bool Odjavi(int idKlijenta)
            => klijenti.TryRemove(idKlijenta, out _);

        public InfoKlijenta PronadjiPoId(int idKlijenta)
            => klijenti.TryGetValue(idKlijenta, out var info) ? info : null;

        public IEnumerable<InfoKlijenta> PronadjiPoTipu(TipOsoblja tip)
            => klijenti.Values.Where(k => k.Tip == tip);
    }
}
