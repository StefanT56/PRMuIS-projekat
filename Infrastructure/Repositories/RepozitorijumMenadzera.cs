using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Core.Models;
using Core.Repositories;

namespace Infrastructure.Repositories
{
    public class RepozitorijumMenadzera : IRepozitorijumMenadzera
    {
        private static readonly ConcurrentDictionary<int, bool> menadzerZauzet
            = new ConcurrentDictionary<int, bool>();
        private static readonly ConcurrentDictionary<int, Rezervacija> brojRezervacije = new ConcurrentDictionary<int, Rezervacija>();

        private static int kljuc_vrednost = 1;//borj rezervacije 
        private static bool inicijalizovano = false;
        private static readonly object brava = new object();

        public RepozitorijumMenadzera(int brojMenadzera)
        {
            if (brojMenadzera < 1)
                throw new ArgumentException("Potrebno je bar 1 menadžer.", nameof(brojMenadzera));

            if (!inicijalizovano)
            {
                lock (brava)
                {
                    if (!inicijalizovano)
                    {
                        for (int i = 1; i <= brojMenadzera; i++)
                        {
                            menadzerZauzet[i] = false;
                        }
                        inicijalizovano = true;
                    }
                }
            }
        }

        public bool DobaviStanjeMenadzera(int idMenadzera)
        {
            ValidirajId(idMenadzera);
            return menadzerZauzet[idMenadzera];
        }

        private void ValidirajId(int idMenadzera)
        {
            if (!menadzerZauzet.ContainsKey(idMenadzera))
                throw new ArgumentOutOfRangeException(
                    nameof(idMenadzera),
                    $"ID menadzera {idMenadzera} nije u opsegu 1..{menadzerZauzet.Count}.");
        }

        public void PostaviStanjeMenadzera(int idMenadzera, bool zauzet)
        {
            ValidirajId(idMenadzera);
            menadzerZauzet[idMenadzera] = zauzet;
        }

        public int DobaviBrojStola(int idRezervacije)
        {
            return brojRezervacije[idRezervacije].BrojStola;
        }


        public DateTime DobaviVremeIsteka(int idRezervacije)
        {
            return brojRezervacije[idRezervacije].VremeIsteka;
        }


        public void DodajNovuRezervaciju(int brojRez, int brojStola, DateTime vremeOd, DateTime vremeDo, int brojGostiju)
        {
            // 🐛 DEBUG: Ispis pre čuvanja
            Console.WriteLine($"[DEBUG RepozitorijumMenadzera] Čuvam rezervaciju #{brojRez}:");
            Console.WriteLine($"  - Sto: {brojStola}");
            Console.WriteLine($"  - VremeOd: {vremeOd:yyyy-MM-dd HH:mm}");
            Console.WriteLine($"  - VremeDo: {vremeDo:yyyy-MM-dd HH:mm}");

            brojRezervacije[brojRez] = new Rezervacija
            {
                BrojRezervacije = brojRez,
                BrojStola = brojStola,
                BrojGostiju = brojGostiju,
                VremeOd = vremeOd,
                VremeDo = vremeDo,
                JeAktivna = true
            };

            var sачuvano = brojRezervacije[brojRez];
            Console.WriteLine($"[DEBUG RepozitorijumMenadzera] Sačuvano:");
            Console.WriteLine($"  - VremeOd: {sачuvano.VremeOd:yyyy-MM-dd HH:mm}");
            Console.WriteLine($"  - VremeDo: {sачuvano.VremeDo:yyyy-MM-dd HH:mm}");
            Console.WriteLine($"  - VremeRezervacije: {sачuvano.VremeRezervacije:yyyy-MM-dd HH:mm}");
            Console.WriteLine($"  - VremeIsteka: {sачuvano.VremeIsteka:yyyy-MM-dd HH:mm}");
        }

        public void ZatraziSlobodanSto(int idMenadzera, int portServera, int brojGostiju, DateTime vremeOd, DateTime vremeDo, int? zeljeniSto)
        {
            if (!menadzerZauzet.ContainsKey(idMenadzera))
                throw new ArgumentException(string.Format("ID menadzera {0} nije u opsegu 1..{1}.", idMenadzera, menadzerZauzet.Count));

            try
            {
                using (var udpKlijent = new UdpClient())
                {
                    udpKlijent.Client.ReceiveTimeout = 5000;

                    var serverEP = new IPEndPoint(IPAddress.Loopback, portServera);

                    // Format poruke sa vremenima
                    string poruka;
                    if (zeljeniSto.HasValue)
                    {
                        poruka = string.Format("GET_TABLE;{0};{1:yyyy-MM-dd HH:mm};{2:yyyy-MM-dd HH:mm};{3}",
                            brojGostiju,
                            vremeOd,
                            vremeDo,
                            zeljeniSto.Value);
                    }
                    else
                    {
                        poruka = string.Format("GET_TABLE;{0};{1:yyyy-MM-dd HH:mm};{2:yyyy-MM-dd HH:mm}",
                            brojGostiju,
                            vremeOd,
                            vremeDo);
                    }

                    byte[] podaci = Encoding.UTF8.GetBytes(poruka);
                    udpKlijent.Send(podaci, podaci.Length, serverEP);

                    IPEndPoint odgovoriEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] odgovor = udpKlijent.Receive(ref odgovoriEP);
                    string odgovorTekst = Encoding.UTF8.GetString(odgovor);

                    if (odgovorTekst.StartsWith("OK;"))
                    {
                        var delovi = odgovorTekst.Split(';');
                        int brojRezervacije = int.Parse(delovi[1]);
                        int brojStola = int.Parse(delovi[2]);

                        Console.WriteLine(string.Format("REZERVACIJA POTVRĐENA! Broj: {0}, Sto: {1}, Vreme: {2:HH:mm}-{3:HH:mm}",
                            brojRezervacije,
                            brojStola,
                            vremeOd,
                            vremeDo));
                    }
                    else
                    {
                        Console.WriteLine("Nema slobodnih stolova za traženi period.");
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(string.Format("Greška pri komunikaciji sa serverom: {0}", ex.Message));
            }
        }

        public void UkloniRezervaciju(int idRezervacije)
        {
            brojRezervacije.TryRemove(idRezervacije, out var rezervacija);
        }

        public IEnumerable<(int Key, Rezervacija Sto)> DobaviIstekleRezervacije()
        {
            var sada = DateTime.Now;
            return brojRezervacije
                .Where(kvp => sada > kvp.Value.VremeIsteka)
                .Select(kvp => (kvp.Key, kvp.Value));
        }

        public bool ProveriRezervaciju(int idRezervacije)
        {
            return brojRezervacije.ContainsKey(idRezervacije);
        }
        public Rezervacija DobaviRezervaciju(int idRezervacije)
        {
            if (brojRezervacije.TryGetValue(idRezervacije, out var rezervacija))
            {
                return rezervacija;
            }
            return null;
        }
        public bool JeLiZauzet(int idMenadzera)
        {
            throw new NotImplementedException();
        }

    }
}
