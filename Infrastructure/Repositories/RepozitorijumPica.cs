using System.Collections.Concurrent;
using System.Collections.Generic;
using Core.Models;
using Core.Repositories;

namespace Infrastructure.Repositories
{
    public class RepozitorijumPica : IRepozitorijumPorudzbina
    {
        private static readonly ConcurrentStack<Porudzbina> stekPica = new ConcurrentStack<Porudzbina>();
        private static readonly object stekBrava = new object();

        public void DodajPorudzbinu(Porudzbina porudzbina)
        {
            lock (stekBrava)
            {
                stekPica.Push(porudzbina);
            }
        }

        public List<Porudzbina> DobaviPorudzbine()
        {
            lock (stekBrava)
            {
                var lista = new List<Porudzbina>();


                //  int brojStavki = System.Math.Min(5, stekPica.Count);
                int brojStavki = stekPica.Count;

                for (int i = 0; i < brojStavki; i++)
                {
                    if (stekPica.TryPop(out Porudzbina p))
                    {
                        lista.Add(p);
                    }
                }
                
                return lista;
            }
        }

        public void ObrisiPorudzbinu(Porudzbina porudzbina)
        {
           //
        }

        public int BrojPorudzbinaNaSteku()
        {
            return stekPica.Count;
        }
    }
}
