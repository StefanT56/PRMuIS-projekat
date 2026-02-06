using System.Collections.Generic;
using Core.Models;

namespace Core.Services
{
    public interface IServisNotifikacija
    {
        void ObavestiOsoblje(TipOsoblja tip, string poruka);
    }

    public interface IServisZauzimanjaStola
    {
        void ZaumiSto();
    }

    public interface IServisOslobadjanja
    {
        void OslobodiSto();
    }

    public interface IServisSlanjaNaPripremu
    {
        void PosaljiPorudzbinu(int idKonobara, int brojStola, List<Porudzbina> porudzbine);
    }

    public interface IServisCitanja
    {
        IEnumerable<Sto> ProcitajStolove();
    }

    public interface INapraviPorudzbinu
    {
        void Naruci(int brojStola, List<Porudzbina> porudzbine);
    }

    public interface IPripremiPorudzbinu
    {
        void Pripremi();
    }
}
