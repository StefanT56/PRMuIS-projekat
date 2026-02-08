using Core.Models;
using Core.Repositories;
using System;
using Application.UI;

namespace Application.Services
{
    public class ServisUpravljanjaMenadzerom
    {
        private readonly IRepozitorijumMenadzera repozitorijumMenadzera;

        public ServisUpravljanjaMenadzerom(IRepozitorijumMenadzera repo)
        {
            repozitorijumMenadzera = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public void ZaumiIliRezervistiSto(int idKlijenta, TipOsoblja tipKlijenta)
        {
            if (tipKlijenta != TipOsoblja.Menadzer)
                return;

            while (true)
            {
                ConsoleUI.Clear();
                ConsoleUI.Header("Menadzer meni");

                ConsoleUI.Option("1", "Napravi rezervaciju");
                ConsoleUI.Option("2", "Proveri rezervaciju");
                ConsoleUI.Option("3", "Gost stigao na rezervaciju");
                ConsoleUI.Option("0", "Ugasi menadzera");
                ConsoleUI.Info("Unesi instrukciju:");

                var key = Console.ReadLine()?.Trim();

                try
                {
                    if (key == "1")
                    {
                        repozitorijumMenadzera.PostaviStanjeMenadzera(idKlijenta, true);

                        // BROJ GOSTIJU
                        ConsoleUI.Info("Unesite broj gostiju (1-10):");
                        if (!int.TryParse(Console.ReadLine(), out var brojGostiju) || brojGostiju > 10 || brojGostiju < 1)
                        {
                            ConsoleUI.Warning("Unesi validan broj gostiju (max 10)!");
                            repozitorijumMenadzera.PostaviStanjeMenadzera(idKlijenta, false);
                            ConsoleUI.Pause();
                            continue;
                        }

                        // VRIJEME OD (početak rezervacije)
                        ConsoleUI.Info("Unesite vreme dolaska (format: HH:mm, npr. 18:30):");
                        string vremeOdUnos = Console.ReadLine();
                        DateTime vremeOd = DateTime.Now;

                        if (!string.IsNullOrWhiteSpace(vremeOdUnos))
                        {
                            try
                            {
                                var parts = vremeOdUnos.Split(':');
                                if (parts.Length == 2)
                                {
                                    int sati = int.Parse(parts[0]);
                                    int minuti = int.Parse(parts[1]);
                                    vremeOd = DateTime.Today.AddHours(sati).AddMinutes(minuti);

                                    if (vremeOd < DateTime.Now)
                                    {
                                        vremeOd = vremeOd.AddDays(1);
                                    }
                                }
                            }
                            catch
                            {
                                ConsoleUI.Warning("Nevalidno vreme OD, koristim trenutno vreme.");
                            }
                        }

                        // VRIJEME DO (kraj rezervacije)
                        ConsoleUI.Info("Unesite vreme kraja rezervacije (format: HH:mm, npr. 20:30):");
                        string vremeDoUnos = Console.ReadLine();
                        DateTime vremeDo = vremeOd.AddHours(2); // Default 2 sata

                        if (!string.IsNullOrWhiteSpace(vremeDoUnos))
                        {
                            try
                            {
                                var parts = vremeDoUnos.Split(':');
                                if (parts.Length == 2)
                                {
                                    int sati = int.Parse(parts[0]);
                                    int minuti = int.Parse(parts[1]);
                                    DateTime temp = DateTime.Today.AddHours(sati).AddMinutes(minuti);

                                    if (temp < vremeOd)
                                    {
                                        temp = temp.AddDays(1);
                                    }

                                    vremeDo = temp;
                                }
                            }
                            catch
                            {
                                ConsoleUI.Warning("Nevalidno vreme DO, koristim +2 sata.");
                            }
                        }

                        // BROJ STOLA (opciono)
                        ConsoleUI.Info("Unesite broj stola (ili Enter za automatsku dodelu):");
                        string stoInput = Console.ReadLine();
                        int? zeljeniSto = null;

                        if (!string.IsNullOrWhiteSpace(stoInput) && int.TryParse(stoInput, out int sto))
                        {
                            zeljeniSto = sto;
                        }

                        ConsoleUI.Info(string.Format("Tražim sto za {0} gostiju ({1:HH:mm}-{2:HH:mm}){3}",
                            brojGostiju, vremeOd, vremeDo, zeljeniSto.HasValue ? (" | Željeni sto: " + zeljeniSto.Value) : ""));

                        repozitorijumMenadzera.ZatraziSlobodanSto(idKlijenta, 4000, brojGostiju, vremeOd, vremeDo, zeljeniSto);

                        repozitorijumMenadzera.PostaviStanjeMenadzera(idKlijenta, false);
                        ConsoleUI.Pause();
                    }
                    else if (key == "2")
                    {
                        repozitorijumMenadzera.PostaviStanjeMenadzera(idKlijenta, true);

                        ConsoleUI.Info("Unesite broj rezervacije:");
                        if (!int.TryParse(Console.ReadLine(), out var brojRezervacije))
                        {
                            ConsoleUI.Warning("Rezervacija mora biti broj!");
                            repozitorijumMenadzera.PostaviStanjeMenadzera(idKlijenta, false);
                            ConsoleUI.Pause();
                            continue;
                        }

                        try
                        {
                            using (var udpKlijent = new System.Net.Sockets.UdpClient())
                            {
                                udpKlijent.Client.ReceiveTimeout = 5000;
                                var serverEP = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 4003);

                                string poruka = string.Format("CHECK_RESERVATION;{0}", brojRezervacije);
                                byte[] podaci = System.Text.Encoding.UTF8.GetBytes(poruka);
                                udpKlijent.Send(podaci, podaci.Length, serverEP);

                                var odgovoriEP = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
                                byte[] odgovor = udpKlijent.Receive(ref odgovoriEP);
                                string odgovorTekst = System.Text.Encoding.UTF8.GetString(odgovor).Trim();

                                if (!odgovorTekst.StartsWith("OK;"))
                                {
                                    ConsoleUI.Warning(string.Format("Rezervacija #{0} ne postoji ili je istekla.", brojRezervacije));
                                    repozitorijumMenadzera.PostaviStanjeMenadzera(idKlijenta, false);
                                    ConsoleUI.Pause();
                                    continue;
                                }

                                // OK;sto;vremeOd;vremeDo
                                var delovi = odgovorTekst.Split(';');
                                int brojStola = int.Parse(delovi[1]);
                                DateTime vremeOd = DateTime.Parse(delovi[2]);
                                DateTime vremeDo = DateTime.Parse(delovi[3]);

                                ConsoleUI.Info(string.Format("Rezervacija #{0} je VALIDNA: Sto {1}, {2:dd.MM HH:mm}-{3:HH:mm}",
                                    brojRezervacije, brojStola, vremeOd, vremeDo));
                            }
                        }
                        catch (Exception ex)
                        {
                            ConsoleUI.Error(string.Format("Greška pri komunikaciji: {0}", ex.Message));
                        }

                        repozitorijumMenadzera.PostaviStanjeMenadzera(idKlijenta, false);
                        ConsoleUI.Pause();
                    }
                    else if (key == "3")
                    {
                        // Gost stigao na rezervaciju (troši rezervaciju na serveru i postavlja sto u ZAUZET)
                        repozitorijumMenadzera.PostaviStanjeMenadzera(idKlijenta, true);

                        ConsoleUI.Info("Unesite broj rezervacije:");
                        if (!int.TryParse(Console.ReadLine(), out var brojRezervacije))
                        {
                            ConsoleUI.Warning("Broj rezervacije mora biti broj!");
                            repozitorijumMenadzera.PostaviStanjeMenadzera(idKlijenta, false);
                            ConsoleUI.Pause();
                            continue;
                        }

                        try
                        {
                            using (var udpKlijent = new System.Net.Sockets.UdpClient())
                            {
                                udpKlijent.Client.ReceiveTimeout = 5000;
                                var serverEP = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 4003);

                                string poruka = string.Format("USE_RESERVATION;{0}", brojRezervacije);
                                byte[] podaci = System.Text.Encoding.UTF8.GetBytes(poruka);
                                udpKlijent.Send(podaci, podaci.Length, serverEP);

                                var odgovoriEP = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
                                byte[] odgovor = udpKlijent.Receive(ref odgovoriEP);
                                string odgovorTekst = System.Text.Encoding.UTF8.GetString(odgovor).Trim();

                                if (odgovorTekst.StartsWith("OK;"))
                                {
                                    var delovi = odgovorTekst.Split(';');
                                    int brojStola = int.Parse(delovi[1]);

                                    ConsoleUI.Info(string.Format("Gosti stigli! Sto {0} je sada ZAUZET.", brojStola));
                                }
                                else
                                {
                                    ConsoleUI.Warning(string.Format("Rezervacija #{0} ne postoji ili je već iskorišćena.", brojRezervacije));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ConsoleUI.Error(string.Format("Greška pri komunikaciji: {0}", ex.Message));
                        }

                        repozitorijumMenadzera.PostaviStanjeMenadzera(idKlijenta, false);
                        ConsoleUI.Pause();
                    }
                    else if (key == "0")
                    {
                        ConsoleUI.Info("Zatvaram menadzera…");
                        return;
                    }
                    else
                    {
                        ConsoleUI.Warning("Unesi 0, 1, 2 ili 3!");
                        ConsoleUI.Pause();
                    }
                }
                catch (Exception ex)
                {
                    ConsoleUI.Error(ex.Message);
                    repozitorijumMenadzera.PostaviStanjeMenadzera(idKlijenta, false);
                    ConsoleUI.Pause();
                }
            }
        }
    }
}
