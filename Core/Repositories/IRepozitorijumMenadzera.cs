using System;
using System.Collections.Generic;
using Core.Models;

namespace Core.Repositories
{
    public interface IRepozitorijumMenadzera
    {
        bool DobaviStanjeMenadzera(int idMenadzera);
        void PostaviStanjeMenadzera(int idMenadzera, bool zauzet);
        int DobaviBrojStola(int idRezervacije);
        DateTime DobaviVremeIsteka(int idRezervacije);
        void DodajNovuRezervaciju(int brojRez, int brojStola, DateTime vremeOd, DateTime vremeDo, int x);
        //  void ZatraziSlobodanSto(int idMenadzera, int portServera, int brojGostiju,DateTime vreme);
        void UkloniRezervaciju(int idRezervacije);
        IEnumerable<(int Key, Rezervacija Sto)> DobaviIstekleRezervacije();
        bool ProveriRezervaciju(int idRezervacije);

        bool JeLiZauzet(int idMenadzera);

        void ZatraziSlobodanSto(int idMenadzera, int portServera, int brojGostiju, DateTime vremeOd, DateTime vremeDo, int? zeljeniSto);

    }
}
