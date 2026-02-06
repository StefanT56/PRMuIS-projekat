using System;
using System.Collections.Generic;
using System.Linq;
using Core.Models;
using Core.Repositories;

namespace Infrastructure.Repositories
{
    public static class RepozitorijumStolova
    {
        private static readonly List<Sto> stolovi;
        private static readonly Random rnd = new Random();

        static RepozitorijumStolova()
        {
            stolovi = new List<Sto>();
            for (int i = 1; i <= 15; i++)
            {
                stolovi.Add(new Sto(
                    broj: i,
                    gosti: rnd.Next(2, 10),
                    stat: Status.slobodan,
                    lista: new List<Porudzbina>()
                ));
            }
        }

        public static IEnumerable<Sto> DobaviSveStolove()
            => stolovi;

        public static void AzurirajSto(Sto sto)
        {
            int indeks = stolovi.FindIndex(s => s.BrojStola == sto.BrojStola);
            if (indeks >= 0)
                stolovi[indeks] = sto;
        }

        public static Sto DobaviPoId(int brojStola)
            => stolovi.FirstOrDefault(s => s.BrojStola == brojStola);

        public static void OcistiSto(int brojStola)
        {
            var noviSto = new Sto(
                broj: brojStola,
                gosti: rnd.Next(2, 10),
                stat: Status.slobodan,
                lista: new List<Porudzbina>()
            );
            AzurirajSto(noviSto);
        }
    }
}
