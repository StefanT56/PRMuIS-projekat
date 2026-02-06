using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using Core.Enums;
using Core.Models;
using Core.Repositories;
using Application.Services;
using Infrastructure.Repositories;

namespace Server
{
    internal class TcpObrade
    {
        private readonly ServisNotifikacija servisNotifikacija;
        private readonly ServisSlanjaNaPripremu servisPripreme;
        private readonly IDirektorijumKlijenata direktorijum;

        public TcpObrade(
            ServisNotifikacija servisNotifikacija,
            ServisSlanjaNaPripremu servisPripreme,
            IDirektorijumKlijenata direktorijum)
        {
            this.servisNotifikacija = servisNotifikacija;
            this.servisPripreme = servisPripreme;
            this.direktorijum = direktorijum;
        }

        public void ObradiRegistraciju(Socket klijent)
        {
            while (true)
            {
                string linija = MrezniSlusaoci.ProcitajTekst(klijent, 2048);
                if (string.IsNullOrWhiteSpace(linija)) return;

                var delovi = linija.Split(';');
                if (delovi.Length < 3 || delovi[0] != "REGISTER") continue;

                int id = int.Parse(delovi[1]);
                TipOsoblja tip = (TipOsoblja)Enum.Parse(typeof(TipOsoblja), delovi[2]);

                direktorijum.Registruj(new InfoKlijenta { Id = id, Tip = tip, Soket = klijent });

                klijent.Send(Encoding.UTF8.GetBytes("OK"));
                Console.WriteLine($"[Server] Klijent prijavljen (ID={id}, Tip={tip}).");
            }
        }

        public void ObradiSpremno(Socket klijent)
        {
            while (true)
            {
                string linija = MrezniSlusaoci.ProcitajTekst(klijent, 8192);
                if (string.IsNullOrWhiteSpace(linija)) return;

                var delovi = linija.Split(';');
                if (delovi.Length < 4 || delovi[0] != "NOTIFY_READY") continue;

                int brStola = int.Parse(delovi[1]);
                int idKonobara = int.Parse(delovi[2]);
                string tipPorudzbine = delovi[3];

                
                var sto = RepozitorijumStolova.DobaviPoId(brStola);
                if (sto != null && sto.Porudzbine != null)
                {
                    Kategorija tip = tipPorudzbine == "hrane" ? Kategorija.hrana : Kategorija.pice;

                    foreach (var p in sto.Porudzbine.Where(x => x.KategorijaArtikla == tip && x.statusArtikla == StatusArtikla.priprema))
                    {
                        p.statusArtikla = StatusArtikla.spremno;
                    }

                    RepozitorijumStolova.AzurirajSto(sto);
                }

                servisNotifikacija.ObvestiGotovost(brStola, idKonobara, tipPorudzbine);
                Server.UI.ServerVizualizacija.DodajDogadjaj(
                    string.Format("Porudzbina spremna (Sto {0}, Konobar {1}, Tip: {2})", brStola, idKonobara, tipPorudzbine));
            }
        }

        public void ObradiPorudzbinu(Socket klijent)
        {
            while (true)
            {
                string linija = MrezniSlusaoci.ProcitajTekst(klijent, 8192);
                if (string.IsNullOrWhiteSpace(linija)) return;

                var delovi = linija.Split(new[] { ';' }, 4);
                if (delovi.Length != 4 || delovi[0] != "ORDER")
                {
                    Console.WriteLine($"[Server] Nevažeća ORDER poruka: {linija}");
                    return;
                }

                int idKonobara = int.Parse(delovi[1]);
                int brojStola = int.Parse(delovi[2]);
                Sto sto = DeserijalizujSto(delovi[3]);

                 
                var postojeciSto = RepozitorijumStolova.DobaviPoId(brojStola);
                if (postojeciSto != null)
                {
                    sto.Stanje = postojeciSto.Stanje;
                    sto.ZauzeoKonobar = postojeciSto.ZauzeoKonobar;

                     
                    foreach (var nova in sto.Porudzbine)
                    {
                        postojeciSto.Porudzbine.Add(nova);
                    }
                    sto.Porudzbine = postojeciSto.Porudzbine;
                }
                 

                RepozitorijumStolova.AzurirajSto(sto);
                Server.UI.ServerVizualizacija.DodajDogadjaj(string.Format("ORDER primljen (Sto {0}, Konobar {1}, {2} stavki)", brojStola, idKonobara, sto.Porudzbine.Count));

                Thread.Sleep(1000);
                foreach (Porudzbina p in RepozitorijumStolova.DobaviPoId(brojStola).Porudzbine)
                    p.statusArtikla = StatusArtikla.priprema;

                servisPripreme.PosaljiPorudzbinu(idKonobara, sto.BrojStola, sto.Porudzbine);
            }
        }

        public void ObradiDostavljeno(Socket klijent)
        {
            while (true)
            {
                string linija = MrezniSlusaoci.ProcitajTekst(klijent, 8192);
                if (string.IsNullOrWhiteSpace(linija)) return;

                var delovi = linija.Split(';');
                if (delovi.Length < 3 || delovi[0] != "DELIVERED") continue;

                int brStola = int.Parse(delovi[1]);
                string tipString = delovi[2];

                var sto = RepozitorijumStolova.DobaviPoId(brStola);
                if (sto == null || sto.Porudzbine == null) continue;

                Kategorija tip = tipString == "hrane" ? Kategorija.hrana : Kategorija.pice;

                 
                var porudzbineZaDostavu = sto.Porudzbine
                    .Where(p => p.KategorijaArtikla == tip && p.statusArtikla == StatusArtikla.spremno)
                    .ToList();

                foreach (var p in porudzbineZaDostavu)
                {
                    p.statusArtikla = StatusArtikla.dostavljeno;
                }
 
                RepozitorijumStolova.AzurirajSto(sto);

                Server.UI.ServerVizualizacija.DodajDogadjaj(
                    string.Format("DELIVERED (Sto {0}, Tip: {1}, {2} stavki)", brStola, tipString, porudzbineZaDostavu.Count));
            }
        }

        public void ObradiRacun(Socket klijent)
        {
            string poruka = MrezniSlusaoci.ProcitajTekst(klijent, 2048);
            if (string.IsNullOrWhiteSpace(poruka)) return;

            var delovi = poruka.Split(';');
            if (delovi.Length < 2 || delovi[0] != "GET_BILL") return;

            int brojStola = int.Parse(delovi[1]);
            var sto = RepozitorijumStolova.DobaviPoId(brojStola);

            double osnovica = 0.0;
            if (sto.Porudzbine != null && sto.Porudzbine.Count > 0)
                osnovica = sto.Porudzbine.Sum(p => p.cena);

            double pdv = Math.Round(osnovica * 0.20, 2);
            double ukupno = Math.Round(osnovica + pdv, 2);

            string odgovor = string.Format("BILL;{0:F2}", ukupno);
            klijent.Send(Encoding.UTF8.GetBytes(odgovor));

            Server.UI.ServerVizualizacija.DodajDogadjaj(
                string.Format("Racun poslat (Sto {0}, Ukupno: {1:F2} RSD)", brojStola, ukupno));
        }

        private Sto DeserijalizujSto(string b64)
        {
            byte[] stoData = Convert.FromBase64String(b64);
            using (var ms = new MemoryStream(stoData))
            {
                var bf = new BinaryFormatter();
                return (Sto)bf.Deserialize(ms);
            }
        }
    }
}
