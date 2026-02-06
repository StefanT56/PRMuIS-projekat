using Core.Enums;
using Core.Models;
using Core.Repositories;
using Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    internal class UdpObrade
    {
        private readonly RepozitorijumMenadzera repozitorijumMenadzera;
        private readonly int udpPortMenadzerNotifikacija;

        public UdpObrade(RepozitorijumMenadzera repozitorijumMenadzera, int udpPortMenadzerNotifikacija)
        {
            this.repozitorijumMenadzera = repozitorijumMenadzera;
            this.udpPortMenadzerNotifikacija = udpPortMenadzerNotifikacija;
        }

        public void ObradiStolove(string poruka, EndPoint udaljeni, Socket udp)
        {
            var delovi = poruka.Split(';');
            if (delovi.Length < 2) return;

            if (delovi[0] == "GET_TABLE")
            {
                // NOVA LOGIKA: Proveri da li ima vremenske parametre (rezervacija) ili ne (zauzimanje)

                if (delovi.Length >= 4)
                {
                    // ✅ GET_TABLE sa vremenskim parametrima = REZERVACIJA od MENADŽERA
                    // Format: GET_TABLE;brGostiju;vremeOd;vremeDo;[zeljeniSto]

                    int brGostiju = int.Parse(delovi[1]);
                    DateTime vremeOd = DateTime.Parse(delovi[2]);
                    DateTime vremeDo = DateTime.Parse(delovi[3]);

                    int? zeljeniSto = null;
                    if (delovi.Length >= 5 && int.TryParse(delovi[4], out int sto))
                    {
                        zeljeniSto = sto;
                    }

                    var slobodni = RepozitorijumStolova.DobaviSveStolove()
                        .Where(s => s.Stanje == Status.slobodan && s.BrojGostiju >= brGostiju)
                        .OrderBy(s => s.BrojGostiju)
                        .ToList();

                    if (zeljeniSto.HasValue)
                    {
                        var specificni = slobodni.FirstOrDefault(s => s.BrojStola == zeljeniSto.Value);
                        if (specificni != null)
                            slobodni = new List<Sto> { specificni };
                        else
                            slobodni.Clear();
                    }

                    if (slobodni.Any())
                    {
                        var stoZaRezervaciju = slobodni.First();
                        stoZaRezervaciju.Stanje = Status.rezervisan;  // ✅ REZERVISAN (ne zauzet)
                        RepozitorijumStolova.AzurirajSto(stoZaRezervaciju);

                        // Kreiraj jedinstveni ID rezervacije
                        int rezervacijaId = Math.Abs(DateTime.Now.GetHashCode() % 100000);

                        repozitorijumMenadzera.DodajNovuRezervaciju(rezervacijaId, stoZaRezervaciju.BrojStola);

                        MrezniSlusaoci.PosaljiUdp(udp, udaljeni, string.Format("OK;{0};{1}", rezervacijaId, stoZaRezervaciju.BrojStola));
                        Server.UI.ServerVizualizacija.DodajDogadjaj(string.Format("Rezervisan sto {0} (Rez #{1}, {2:HH:mm}-{3:HH:mm})",
                            stoZaRezervaciju.BrojStola, rezervacijaId, vremeOd, vremeDo));
                    }
                    else
                    {
                        MrezniSlusaoci.PosaljiUdp(udp, udaljeni, "ERROR;Nema slobodnih stolova za rezervaciju");
                        Server.UI.ServerVizualizacija.DodajDogadjaj(string.Format("Nema stolova za rezervaciju ({0} gostiju)", brGostiju));
                    }
                }
                else
                {
                    // ✅ GET_TABLE bez vremenskih parametara = ZAUZIMANJE od KONOBARA
                    // Format: GET_TABLE;brGostiju

                    int brGostiju = int.Parse(delovi[1]);

                    var slobodni = RepozitorijumStolova.DobaviSveStolove()
                        .Where(s => s.Stanje == Status.slobodan && s.BrojGostiju >= brGostiju)
                        .OrderBy(s => s.BrojGostiju)
                        .ToList();

                    if (slobodni.Any())
                    {
                        var sto = slobodni.First();
                        sto.Stanje = Status.zauzet; // ✅ ZAUZET za goste koji su tu
                        RepozitorijumStolova.AzurirajSto(sto);

                        MrezniSlusaoci.PosaljiUdp(udp, udaljeni, string.Format("OK;{0}", sto.BrojStola));
                        Server.UI.ServerVizualizacija.DodajDogadjaj(string.Format("Dodeljen sto {0} ({1} gostiju)", sto.BrojStola, brGostiju));
                    }
                    else
                    {
                        MrezniSlusaoci.PosaljiUdp(udp, udaljeni, "ERROR;Nema slobodnih stolova");
                        Server.UI.ServerVizualizacija.DodajDogadjaj(string.Format("Nema slobodnih stolova za {0} gostiju", brGostiju));
                    }
                }
                return;
            }

            if (delovi[0] == "TAKE_TABLE")
            {
                // STARA LOGIKA - ostaje kao backup
                if (delovi.Length < 5)
                {
                    MrezniSlusaoci.PosaljiUdp(udp, udaljeni, "TABLE_BUSY");
                    return;
                }

                int brGostiju = int.Parse(delovi[2]);
                int brojRezervacije = int.Parse(delovi[4]);

                var slobodni = RepozitorijumStolova.DobaviSveStolove()
                    .Where(s => s.Stanje == Status.slobodan && s.BrojGostiju >= brGostiju)
                    .OrderBy(s => s.BrojGostiju)
                    .ToList();

                if (slobodni.Any())
                {
                    var sto = slobodni.First();
                    sto.Stanje = Status.rezervisan;
                    RepozitorijumStolova.AzurirajSto(sto);

                    repozitorijumMenadzera.DodajNovuRezervaciju(brojRezervacije, sto.BrojStola);

                    MrezniSlusaoci.PosaljiUdp(udp, udaljeni, string.Format("TABLE_FREE;{0}", sto.BrojStola));
                    Server.UI.ServerVizualizacija.DodajDogadjaj(string.Format("Rezervisan sto {0} (Rezervacija #{1})", sto.BrojStola, brojRezervacije));
                }
                else
                {
                    MrezniSlusaoci.PosaljiUdp(udp, udaljeni, "TABLE_BUSY");
                    Server.UI.ServerVizualizacija.DodajDogadjaj(string.Format("Nema stolova za rezervaciju #{0}", brojRezervacije));
                }
            }
        }

        public void ObradiOtkazivanje(string poruka, EndPoint udaljeni, Socket udp)
        {
            var delovi = poruka.Split(';');
            if (delovi.Length < 2) return;
            if (delovi[0] != "CANCEL_TABLE") return;

            int brojStola = int.Parse(delovi[1]);
            RepozitorijumStolova.OcistiSto(brojStola);

            MrezniSlusaoci.PosaljiUdp(udp, udaljeni, "OK");
            Server.UI.ServerVizualizacija.DodajDogadjaj($"Oslobodjen sto {brojStola}");
        }

        public void ObradiProveruRezervacije(string poruka, EndPoint udaljeni, Socket udp)
        {
            if (!int.TryParse(poruka.Trim(), out int rezervacijaKod))
            {
                MrezniSlusaoci.PosaljiUdp(udp, udaljeni, "ERROR;Invalid reservation");
                Console.WriteLine("[Rezervacije] Nevažeća poruka (nije broj).");
                return;
            }

            bool validna = repozitorijumMenadzera.ProveriRezervaciju(rezervacijaKod);
            if (!validna)
            {
                MrezniSlusaoci.PosaljiUdp(udp, udaljeni, "ERROR;Invalid reservation");
                Console.WriteLine($"[Rezervacije] Rezervacija #{rezervacijaKod} nije validna.");
                return;
            }

            int brStola = repozitorijumMenadzera.DobaviBrojStola(rezervacijaKod);

             var sto = RepozitorijumStolova.DobaviPoId(brStola);
            if (sto != null && sto.Stanje == Status.rezervisan)
            {
                sto.Stanje = Status.zauzet;
                RepozitorijumStolova.AzurirajSto(sto);
            }

            repozitorijumMenadzera.UkloniRezervaciju(rezervacijaKod);

            MrezniSlusaoci.PosaljiUdp(udp, udaljeni, $"OK;{brStola}");
            Server.UI.ServerVizualizacija.DodajDogadjaj($"Rezervacija #{rezervacijaKod} potvrdjena - Sto {brStola} sada ZAUZET");

            ObavestiMenadzera(rezervacijaKod);
        }

        private void ObavestiMenadzera(int rezervacijaKod)
        {
            try
            {
                using (var notify = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    var menadzerEp = new IPEndPoint(IPAddress.Loopback, udpPortMenadzerNotifikacija);
                    string poruka = $"RESERVATION_USED;{rezervacijaKod}";
                    notify.SendTo(Encoding.UTF8.GetBytes(poruka), menadzerEp);
                }

                Console.WriteLine($"[Rezervacije] Menadžer obavešten (iskorišćena #{rezervacijaKod}).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Rezervacije] Ne mogu da obavestim menadžera: {ex.Message}");
            }
        }
    }
}