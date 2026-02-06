using System.Collections.Generic;
using Core.Models;

namespace Core.Repositories
{
    public interface IRepozitorijumPorudzbina
    {
        void DodajPorudzbinu(Porudzbina porudzbina);
        List<Porudzbina> DobaviPorudzbine();
        void ObrisiPorudzbinu(Porudzbina porudzbina);
        int BrojPorudzbinaNaSteku();
    }
}
