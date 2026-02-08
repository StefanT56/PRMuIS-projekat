using System.Collections.Generic;
using Core.Models;

namespace Core.Repositories
{
    public interface IDirektorijumKlijenata
    {
        void Registruj(InfoKlijenta klijent);
        bool Odjavi(int idKlijenta);
        InfoKlijenta PronadjiPoId(int idKlijenta);
        IEnumerable<InfoKlijenta> PronadjiPoTipu(TipOsoblja tip);
    }
}
