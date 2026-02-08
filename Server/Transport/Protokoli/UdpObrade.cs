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

                    // 🐛 DEBUG: Ispis parsiranih vremena
                    Console.WriteLine(string.Format("[DEBUG] Parsirana vremena: vremeOd={0:yyyy-MM-dd HH:mm}, vremeDo={1:yyyy-MM-dd HH:mm}", vremeOd, vremeDo));

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

                        // Kreiraj jedinstveni ID rezervacije
                        int rezervacijaId = Math.Abs(DateTime.Now.GetHashCode() % 100000);

                        // Veži sto sa rezervacijom da ServerVizualizacija može da prikaže vreme.
                        stoZaRezervaciju.RezervacijaId = rezervacijaId;
                        RepozitorijumStolova.AzurirajSto(stoZaRezervaciju);

                        // Čuvamo rezervaciju na serveru (repo koristi process-wide storage)
                        repozitorijumMenadzera.DodajNovuRezervaciju(rezervacijaId, stoZaRezervaciju.BrojStola, vremeOd, vremeDo, brGostiju);

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
                        var stoZaZauzimanje = slobodni.First();
                        stoZaZauzimanje.Stanje = Status.zauzet;
                        RepozitorijumStolova.AzurirajSto(stoZaZauzimanje);

                        MrezniSlusaoci.PosaljiUdp(udp, udaljeni, string.Format("OK;{0}", stoZaZauzimanje.BrojStola));
                        Server.UI.ServerVizualizacija.DodajDogadjaj(string.Format("Zauzet sto {0} (konobar)", stoZaZauzimanje.BrojStola));
                    }
                    else
                    {
                        MrezniSlusaoci.PosaljiUdp(udp, udaljeni, "ERROR;Nema slobodnih stolova");
                        Server.UI.ServerVizualizacija.DodajDogadjaj(string.Format("Nema stolova za zauzimanje ({0} gostiju)", brGostiju));
                    }
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
            Server.UI.ServerVizualizacija.DodajDogadjaj(string.Format("Oslobodjen sto {0}", brojStola));
        }

        public void ObradiProveruRezervacije(string poruka, EndPoint udaljeni, Socket udp)
        {
            poruka = (poruka ?? string.Empty).Trim();


            string komanda = "USE_RESERVATION";
            int rezervacijaKod;

            if (poruka.Contains(";"))
            {
                var delovi = poruka.Split(';');
                if (delovi.Length < 2 || !int.TryParse(delovi[1], out rezervacijaKod))
                {
                    MrezniSlusaoci.PosaljiUdp(udp, udaljeni, "ERROR;Invalid request");
                    Console.WriteLine("[Rezervacije] Nevažeća poruka (komanda;id).");
                    return;
                }

                komanda = delovi[0].Trim().ToUpperInvariant();
            }
            else
            {
                if (!int.TryParse(poruka, out rezervacijaKod))
                {
                    MrezniSlusaoci.PosaljiUdp(udp, udaljeni, "ERROR;Invalid request");
                    Console.WriteLine("[Rezervacije] Nevažeća poruka (nije broj).");
                    return;
                }
            }

            bool postoji = repozitorijumMenadzera.ProveriRezervaciju(rezervacijaKod);
            if (!postoji)
            {
                MrezniSlusaoci.PosaljiUdp(udp, udaljeni, "ERROR;NotFound");
                Console.WriteLine(string.Format("[Rezervacije] Rezervacija #{0} ne postoji.", rezervacijaKod));
                return;
            }

            if (komanda == "CHECK_RESERVATION")
            {
                var rez = repozitorijumMenadzera.DobaviRezervaciju(rezervacijaKod);
                if (rez == null)
                {
                    MrezniSlusaoci.PosaljiUdp(udp, udaljeni, "ERROR;NotFound");
                    return;
                }

                // OK;sto;vremeOd;vremeDo
                string resp = string.Format("OK;{0};{1:yyyy-MM-dd HH:mm};{2:yyyy-MM-dd HH:mm}",
                    rez.BrojStola, rez.VremeOd, rez.VremeDo);

                MrezniSlusaoci.PosaljiUdp(udp, udaljeni, resp);
                Console.WriteLine(string.Format("[Rezervacije] CHECK ok #{0} -> Sto {1} ({2:HH:mm}-{3:HH:mm})",
                    rezervacijaKod, rez.BrojStola, rez.VremeOd, rez.VremeDo));
                return;
            }

            // USE_RESERVATION (gost stigao)
            int brStola = repozitorijumMenadzera.DobaviBrojStola(rezervacijaKod);

            var sto = RepozitorijumStolova.DobaviPoId(brStola);
            if (sto != null && sto.Stanje == Status.rezervisan)
            {
                sto.Stanje = Status.zauzet;
                sto.RezervacijaId = null;
                RepozitorijumStolova.AzurirajSto(sto);
            }

            repozitorijumMenadzera.UkloniRezervaciju(rezervacijaKod);

            MrezniSlusaoci.PosaljiUdp(udp, udaljeni, string.Format("OK;{0}", brStola));
            Server.UI.ServerVizualizacija.DodajDogadjaj(string.Format("Gost stigao - Rezervacija #{0} -> Sto {1} sada ZAUZET", rezervacijaKod, brStola));

            ObavestiMenadzera(rezervacijaKod);
        }

        private void ObavestiMenadzera(int rezervacijaKod)
        {
            try
            {
                using (var notify = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    var menadzerEp = new IPEndPoint(IPAddress.Loopback, udpPortMenadzerNotifikacija);
                    string poruka = string.Format("RESERVATION_USED;{0}", rezervacijaKod);
                    notify.SendTo(Encoding.UTF8.GetBytes(poruka), menadzerEp);
                }

                Console.WriteLine(string.Format("[Rezervacije] Menadžer obavešten (iskorišćena #{0}).", rezervacijaKod));
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("[Rezervacije] Ne mogu da obavestim menadžera: {0}", ex.Message));
            }
        }
    }
}
