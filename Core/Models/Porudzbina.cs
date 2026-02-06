using Core.Enums;
using System;

namespace Core.Models
{
    
   

    [Serializable]
    public class Porudzbina
    {
        public string nazivArtikla;
        public Kategorija kategorijaArtikla;
        public double cena;
        public StatusArtikla statusArtikla;
        public int idKonobara { get; set; }
        public int brojStola { get; set; }

        public Porudzbina(string naziv, Kategorija kat, double price, StatusArtikla status, int konobar, int sto)
        {
            nazivArtikla = naziv;
            kategorijaArtikla = kat;
            cena = price;
            statusArtikla = status;
            idKonobara = konobar;
            brojStola = sto;
        }

        public Kategorija KategorijaArtikla
        {
            get { return kategorijaArtikla; }
            set { kategorijaArtikla = value; }
        }

        public override string ToString()
        {
            return $"| {nazivArtikla,-14} | {kategorijaArtikla,-18} | {cena,-14} | {statusArtikla,-10} |";
        }
    }
}
