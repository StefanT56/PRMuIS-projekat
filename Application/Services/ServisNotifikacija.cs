using System.Text;
using Core.Models;
using Core.Repositories;
using Core.Services;

namespace Application.Services
{
    public class ServisNotifikacija : IServisNotifikacija
    {
        private readonly IDirektorijumKlijenata direktorijum;

        public ServisNotifikacija(IDirektorijumKlijenata dir)
        {
            direktorijum = dir;
        }

        public void ObavestiOsoblje(TipOsoblja tip, string poruka)
        {
            var osoblje = direktorijum.PronadjiPoTipu(tip);
            foreach (var osoba in osoblje)
            {
                if (osoba != null)
                {
                    var porukaBajt = System.Text.Encoding.UTF8.GetBytes(poruka);
                    osoba.Soket.Send(porukaBajt);
                }
            }
        }

        public void ObvestiGotovost(int brStola, int idKonobara, string tipPorudzbine)  // tacka 5 slanje notifikacijen konobaru
        {
            var konobar = direktorijum.PronadjiPoId(idKonobara);
            if (konobar != null)
            {
                var msg = $"READY;{brStola};{idKonobara};{tipPorudzbine}\n";
                var data = System.Text.Encoding.UTF8.GetBytes(msg);
                konobar.Soket.Send(data);
            }
        }
    }
}
