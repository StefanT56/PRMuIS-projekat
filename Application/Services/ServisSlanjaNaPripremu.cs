using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using Core.Enums;
using Core.Models;
using Core.Repositories;
using Core.Services;

namespace Application.Services
{
    public class ServisSlanjaNaPripremu : IServisSlanjaNaPripremu
    {
        private readonly IDirektorijumKlijenata direktorijum;
        private readonly IRepozitorijumPorudzbina repozitorijumHrane;
        private readonly IRepozitorijumPorudzbina repozitorijumPica;

        public ServisSlanjaNaPripremu(
            IDirektorijumKlijenata dir,
            IRepozitorijumPorudzbina hrana,
            IRepozitorijumPorudzbina pice)
        {
            direktorijum = dir;
            repozitorijumHrane = hrana;
            repozitorijumPica = pice;

            new Thread(() => ObradiZaOsoblje(repozitorijumHrane, TipOsoblja.Kuvar))  // takca 5 pokretanje thredova za dispatch 
            {
                IsBackground = true
            }.Start();

            new Thread(() => ObradiZaOsoblje(repozitorijumPica, TipOsoblja.Barmen))
            {
                IsBackground = true
            }.Start();
        }

        public void PosaljiPorudzbinu(int idKonobara, int brojStola, List<Porudzbina> porudzbine)
        {
            foreach (var p in porudzbine)
            {
                p.idKonobara = idKonobara;
                p.brojStola = brojStola;
            }


            var hrana = porudzbine.Where(p => p.KategorijaArtikla == Kategorija.hrana).ToList();
            foreach (var h in hrana)
            {
                repozitorijumHrane.DodajPorudzbinu(h);
                Console.WriteLine($"[SERVER] Dodato na stek hrane: {h.nazivArtikla} (ukupno na steku: {repozitorijumHrane.BrojPorudzbinaNaSteku()})");
            }

            var pice = porudzbine.Where(p => p.KategorijaArtikla == Kategorija.pice).ToList();
            foreach (var p in pice)
            {
                repozitorijumPica.DodajPorudzbinu(p);
                Console.WriteLine($"[SERVER] Dodato na stek pića: {p.nazivArtikla} (ukupno na steku: {repozitorijumPica.BrojPorudzbinaNaSteku()})");
            }
        }

        public void ObradiZaOsoblje(IRepozitorijumPorudzbina repo, TipOsoblja tip) //TACKA 5 
        {
            Console.WriteLine($"[SERVER] Pokrećem dispatch thread za {tip}");
            var rnd = new Random();
            var formatter = new BinaryFormatter();

            while (true)
            {

                int brojNaSteku = repo.BrojPorudzbinaNaSteku();

                if (brojNaSteku < 1)
                {
                    Thread.Sleep(100);
                    continue;
                }

                Console.WriteLine(string.Format("[SERVER] Stek za {0} ima {1} stavki - procesuiram batch", tip, brojNaSteku));

                var listaPorudzbina = repo.DobaviPorudzbine();
                if (listaPorudzbina == null || listaPorudzbina.Count == 0)
                {
                    Thread.Sleep(50);
                    continue;
                }

                var klijenti = direktorijum
                    .PronadjiPoTipu(tip)
                    .ToList();

                if (klijenti.Count == 0)
                {
                    Console.WriteLine($"[SERVER] Nema aktivnih {tip}-a za dispatch.");
                    Thread.Sleep(200);
                    continue;
                }

                // Grupiši porudžbine po stolu i konobaru
                var grupisano = listaPorudzbina
                    .GroupBy(p => new { p.brojStola, p.idKonobara })
                    .ToList();

                foreach (var grupa in grupisano)
                {
                    int brStola = grupa.Key.brojStola;
                    int idKon = grupa.Key.idKonobara;
                    var porudzbineGrupe = grupa.ToList();

                    byte[] podaci = new byte[8192];
                    using (var ms = new MemoryStream())                           // TACKA 3 
                    {
                        formatter.Serialize(ms, porudzbineGrupe);
                        podaci = ms.ToArray();
                    }

                    string b64 = Convert.ToBase64String(podaci);
                    string protokol = $"PREPARE;{brStola};{idKon};{b64}\n";
                    byte[] payload = Encoding.UTF8.GetBytes(protokol);

                    var klijent = klijenti[rnd.Next(klijenti.Count)];  //radnom dodela slobodnom resursu
                    try
                    {
                        klijent.Soket.Send(payload);
                        Console.WriteLine(
                            $"[SERVER] Poslato {tip}#{klijent.Id}: PREPARE za sto {brStola} ({porudzbineGrupe.Count} stavki)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"[ERROR] Neuspešno slanje {tip}#{klijent.Id}: {ex.Message}");
                    }
                }
            }
        }
    }
}
