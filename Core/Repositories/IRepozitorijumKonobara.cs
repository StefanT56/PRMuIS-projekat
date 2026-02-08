using System.Collections.Generic;

namespace Core.Repositories
{
    public interface IRepozitorijumKonobara
    {
        void DodajKonobara(int id);
        List<int> DobaviKonobare();
        void ObrisiKonobara(int id);
    }
}
