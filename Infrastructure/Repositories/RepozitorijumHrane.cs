using System.Collections.Concurrent;
using System.Collections.Generic;
using Core.Models;
using Core.Repositories;

namespace Infrastructure.Repositories
{
    public class RepozitorijumHrane : IRepozitorijumPorudzbina
    {
        private static readonly ConcurrentStack<Porudzbina> stekHrane = new ConcurrentStack<Porudzbina>(); // TACKA 5 
        private static readonly object stekBrava = new object();

        public void DodajPorudzbinu(Porudzbina porudzbina)
        {
            lock (stekBrava)
            {
                stekHrane.Push(porudzbina);
            }
        }

        public List<Porudzbina> DobaviPorudzbine()
        {
            lock (stekBrava)
            {
                var lista = new List<Porudzbina>();

                // Uzmi do 5 stavki sa steka (ili sve što ima)
                // int brojStavki = System.Math.Min(5, stekHrane.Count);
                int brojStavki = stekHrane.Count;

                for (int i = 0; i < brojStavki; i++)
                {
                    if (stekHrane.TryPop(out Porudzbina p))
                    {
                        lista.Add(p);
                    }
                }
                
                return lista;
            }
        }

        public void ObrisiPorudzbinu(Porudzbina porudzbina)
        {
            // Ne implementiram jer nije potrebno
        }

        public int BrojPorudzbinaNaSteku()
        {
            return stekHrane.Count;
        }
    }
}
