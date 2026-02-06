using System.Collections.Generic;
using Core.Models;

namespace Core.Repositories
{
    public interface IRepozitorijumStolova
    {
        IEnumerable<Sto> DobaviSveStolove();
        void AzurirajSto(Sto sto);
        Sto DobaviPoId(int brojStola);
        void OcistiSto(int brojStola);
    }
}
