using System;
using System.Collections.Generic;

namespace Core.Models
{
    public enum Status { slobodan, zauzet, rezervisan };

    [Serializable]
    public class Sto
    {
        private int brojStola;
        private Status stanje;
        private int brojGostiju;
        private int zauzeoKonobar;
        private List<Porudzbina> porudzbine = new List<Porudzbina>();
        public int? RezervacijaId { get; set; }


        public int BrojStola
        {
            get { return brojStola; }
            set { brojStola = value; }
        }

        public Status Stanje
        {
            get { return stanje; }
            set { stanje = value; }
        }

        public int BrojGostiju
        {
            get { return brojGostiju; }
            set { brojGostiju = value; }
        }

        public int ZauzeoKonobar
        {
            get { return zauzeoKonobar; }
            set { zauzeoKonobar = value; }
        }

        public List<Porudzbina> Porudzbine
        {
            get { return porudzbine; }
            set { porudzbine = value; }
        }

        public Sto(int broj, int gosti, Status stat, List<Porudzbina> lista)
        {
            brojStola = broj;
            brojGostiju = gosti;
            stanje = stat;
            porudzbine = lista;
        }

        public override string ToString()
        {
            string rezultat = "\n------------------------------------------------------------------\n";
            rezultat += "|    Broj stola      |    Broj gostiju        |    Stanje stola   |\n";
            rezultat += $"|    {brojStola,-12}    |    {brojGostiju,-16}    |    {stanje,-11}   |\n";
            rezultat += "------------------------------------------------------------------\n";
            rezultat += "|                            PORUDŽBINE                            |\n";
            rezultat += "------------------------------------------------------------------\n";
            rezultat += "| Naziv artikla    | Kategorija artikla     | Cena artikla    |   Status   |\n";

            foreach (Porudzbina p in porudzbine)
            {
                rezultat += p.ToString() + "\n";
            }
            return rezultat += "------------------------------------------------------------------\n";
        }
    }
}
