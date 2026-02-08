using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using Application.UI;
using Core.Enums;
using Core.Models;
using Infrastructure.Repositories;

namespace StaffClient
{
    class Konobar
    {
        private static readonly object konzolaBrava = new object();
        private static int idKonobara;
        private static int udpLuka;
        private static Socket tcpSoket;
        private static Socket dostavaSoket;

        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Greska pri pokretanju konobara");
                return;
            }

            idKonobara = int.Parse(args[0]);
            int ukupnoKonobara = int.Parse(args[1]);
            udpLuka = int.Parse(args[2]);

            ConsoleUI.Info($"Konobar id={idKonobara}, port {udpLuka}");

            // TCP registracija i notifikacije konobara na port 5000 tacka 2 
            const string serverIp = "127.0.0.1";
            const int serverPort = 5000;

            new Thread(() =>
            {
                try
                {
                    tcpSoket = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp);
                    tcpSoket.Connect(new IPEndPoint(IPAddress.Parse(serverIp), serverPort));

                    dostavaSoket = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp);
                    dostavaSoket.Connect(new IPEndPoint(IPAddress.Parse(serverIp), 4011));

                    string regMsg = $"REGISTER;{idKonobara};Konobar;{udpLuka}\n"; // tacka 2 konobar salje podatke i potvrdu 
                    tcpSoket.Send(Encoding.UTF8.GetBytes(regMsg));

                    byte[] ackbytes = new byte[1024];
                    int bytesRecieved = tcpSoket.Receive(ackbytes);
                    string ack = Encoding.UTF8.GetString(ackbytes, 0, bytesRecieved).Trim();

                    if (ack != "OK")
                    {
                        ConsoleUI.Error("Registracija neuspješna");
                    }
                    else
                    {
                        ConsoleUI.Info("Uspješno registrovan, čekam porudžbine");
                    }

                    // TACKA 5 priem redi poruke 
                    while (true)
                    {
                        byte[] buffer = new byte[1024];
                        int r = tcpSoket.Receive(buffer);

                        string line = Encoding.UTF8.GetString(buffer, 0, r).Trim();

                        if (line.StartsWith("READY;"))
                        {
                            var parts = line.Split(';');
                            int brStola = int.Parse(parts[1]);
                            string tipPorudzbine = parts[3];

                            ConsoleUI.Info($"Porudžbina za sto {brStola} je spremna! Nosim...");
                            Thread.Sleep(1500);
                            ConsoleUI.Info($"Porudžbina {tipPorudzbine} za sto {brStola} je dostavljena.");
                            dostavaSoket.Send(Encoding.UTF8.GetBytes($"DELIVERED;{brStola};{tipPorudzbine}"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NotificationThread ERROR] {ex.Message}");
                }
            })
            { IsBackground = true }.Start();


            Thread glavniThread = new Thread(() => GlavniMeni());
            glavniThread.Start();
        }

        static void GlavniMeni()
        {
            while (true)
            {
                ConsoleUI.Clear();
                ConsoleUI.Header("Konobar menu");

                ConsoleUI.Option("1", "Zauzmi sto");
                ConsoleUI.Option("2", "Naruci");
                ConsoleUI.Option("3", "Racun");
                ConsoleUI.Option("4", "Oslobodi sto");
                ConsoleUI.Option("5", "Potvrdi rezervaciju (gost stigao)");

                ConsoleUI.Option("0", "Izlaz");

                ConsoleUI.Info("Izaberite opciju:");

                string izbor = Console.ReadLine();

                switch (izbor)
                {
                    case "1":
                        ZaumiSto();
                        break;
                    case "2":
                        NapravljPorudzbinu();
                        break;
                    case "3":
                        ZatraziRacun();
                        break;
                    case "4":
                        OslobodiSto();
                        break;
                    case "5":
                        PotvrdRezervaciju();
                        break;
                    case "0":
                        Environment.Exit(0);
                        break;
                    default:
                        ConsoleUI.Warning("Nepoznata opcija!");
                        ConsoleUI.Pause();
                        break;
                }
            }
        }

        static void ZaumiSto() //tacka 2 
        {
            ConsoleUI.Info("Unesite broj gostiju:");
            int brGostiju = int.Parse(Console.ReadLine());

            try
            {
                Socket udpSoket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                string poruka = $"GET_TABLE;{brGostiju}";
                byte[] podaci = Encoding.UTF8.GetBytes(poruka);

                IPEndPoint serverEp = new IPEndPoint(IPAddress.Loopback, 4000);
                udpSoket.SendTo(podaci, serverEp);

                EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
                byte[] bafer = new byte[512];
                int primljeno = udpSoket.ReceiveFrom(bafer, ref remoteEp);

                string odgovor = Encoding.UTF8.GetString(bafer, 0, primljeno).Trim();
                var delovi = odgovor.Split(';');

                if (delovi[0] == "OK")
                {
                    int brStola = int.Parse(delovi[1]);
                    ConsoleUI.Info($"Dodeljen sto broj: {brStola}");
                }
                else
                {
                    ConsoleUI.Error(delovi[1]);
                }

                udpSoket.Close();
            }
            catch (Exception ex)
            {
                ConsoleUI.Error(ex.Message);
                ConsoleUI.Pause();
            }
        }

        static void NapravljPorudzbinu()                  //test tacka 2   I TACKA 3  TACKA 4 
        {
            Console.Write("Unesite broj stola: ");
            int brStola = int.Parse(Console.ReadLine());

            var porudzbine = new List<Porudzbina>();

            while (true)
            {
                ConsoleUI.Info("Dodaj stavku:");
                ConsoleUI.Info("Naziv artikla (ili 'kraj' za završetak):");
                string naziv = Console.ReadLine();
                if (naziv.ToLower() == "kraj") break;

                Console.Write("Kategorija (1-hrana, 2-pice): ");
                int kat = int.Parse(Console.ReadLine());
                Kategorija kategorija = kat == 1 ? Kategorija.hrana : Kategorija.pice;

                Console.Write("Cena: ");
                double cena = double.Parse(Console.ReadLine());

                porudzbine.Add(new Porudzbina(naziv, kategorija, cena, StatusArtikla.priprema, idKonobara, brStola));
            }

            if (porudzbine.Count == 0)
            {
                ConsoleUI.Warning("Nema stavki za slanje!");
                ConsoleUI.Pause();
                return;
            }

            try
            {
                var sto = RepozitorijumStolova.DobaviPoId(brStola);
                if (sto == null)
                {
                    ConsoleUI.Error("Sto ne postoji!");
                    ConsoleUI.Pause();
                    return;
                }

                // Očisti stare porudžbine iz lokalnog stola pre slanja.
                // Lokalni RepozitorijumStolova nema ažurirane statuse sa servera,
                // pa šaljemo SAMO nove porudžbine unutar Sto objekta.
                sto.Porudzbine.Clear();
                sto.Porudzbine.AddRange(porudzbine);
                sto.ZauzeoKonobar = idKonobara;

                byte[] stoData;                          /////////////// TACKA 3 
                using (var ms = new MemoryStream())
                {
                    var bf = new BinaryFormatter();
                    bf.Serialize(ms, sto);
                    stoData = ms.ToArray();
                }

                string b64 = Convert.ToBase64String(stoData);
                string poruka = $"ORDER;{idKonobara};{brStola};{b64}";
                byte[] podaci = Encoding.UTF8.GetBytes(poruka);

                Socket orderSoket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                orderSoket.Connect(new IPEndPoint(IPAddress.Loopback, 15000));
                orderSoket.Send(podaci);
                orderSoket.Close();

                ConsoleUI.Info("Porudžbina poslata!");
                ConsoleUI.Pause();
            }
            catch (Exception ex)
            {
                ConsoleUI.Error(ex.Message);
                ConsoleUI.Pause();
            }
        }

        static void ZatraziRacun()
        {
            ConsoleUI.Info("Unesite broj stola:");
            if (!int.TryParse(Console.ReadLine(), out int brStola))
            {
                ConsoleUI.Error("Broj stola mora biti broj!");
                ConsoleUI.Pause();
                return;
            }

            try
            {
                Socket soket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                soket.Connect(new IPEndPoint(IPAddress.Loopback, 4002));

                string poruka = string.Format("GET_BILL;{0}", brStola);
                byte[] podaci = Encoding.UTF8.GetBytes(poruka);
                soket.Send(podaci);

                byte[] bafer = new byte[2048];
                int primljeno = soket.Receive(bafer);
                string odgovor = Encoding.UTF8.GetString(bafer, 0, primljeno).Trim();

                var delovi = odgovor.Split(';');
                if (delovi.Length >= 2 && delovi[0] == "BILL")
                {
                    double ukupno = double.Parse(delovi[1]);

                    Console.WriteLine();
                    Console.WriteLine("╔════════════════════════════════════════╗");
                    Console.WriteLine("║            RAČUN - STO {0,-2}            ║", brStola);
                    Console.WriteLine("╠════════════════════════════════════════╣");
                    Console.WriteLine("║  UKUPNO:          {0,10:F2} RSD    ║", ukupno);
                    Console.WriteLine("╚════════════════════════════════════════╝");
                    Console.WriteLine();
                }
                else
                {
                    ConsoleUI.Error("Nevalidan odgovor od servera!");
                }

                soket.Close();
            }
            catch (Exception ex)
            {
                ConsoleUI.Error(string.Format("Greška: {0}", ex.Message));
            }

            ConsoleUI.Pause();
        }

        static void OslobodiSto()
        {
            Console.Write("Unesite broj stola: ");
            int brStola = int.Parse(Console.ReadLine());

            try
            {
                Socket udpSoket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                string poruka = $"CANCEL_TABLE;{brStola}";
                byte[] podaci = Encoding.UTF8.GetBytes(poruka);

                IPEndPoint serverEp = new IPEndPoint(IPAddress.Loopback, 4001);
                udpSoket.SendTo(podaci, serverEp);

                EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
                byte[] bafer = new byte[512];
                int primljeno = udpSoket.ReceiveFrom(bafer, ref remoteEp);

                string odgovor = Encoding.UTF8.GetString(bafer, 0, primljeno).Trim();
                if (odgovor == "OK")
                {
                    ConsoleUI.Info($"Sto {brStola} je oslobođen");
                    ConsoleUI.Pause();
                }

                udpSoket.Close();
            }
            catch (Exception ex)
            {
                ConsoleUI.Error(ex.Message);
                ConsoleUI.Pause();
            }


        }
        static void PotvrdRezervaciju()
        {
            ConsoleUI.Info("Unesite kod rezervacije:");
            if (!int.TryParse(Console.ReadLine(), out int kodRezervacije))
            {
                ConsoleUI.Error("Kod mora biti broj!");
                ConsoleUI.Pause();
                return;
            }

            try
            {
                Socket udpSoket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                string poruka = kodRezervacije.ToString();
                byte[] podaci = Encoding.UTF8.GetBytes(poruka);

                IPEndPoint serverEp = new IPEndPoint(IPAddress.Loopback, 4003);
                udpSoket.SendTo(podaci, serverEp);

                EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
                byte[] bafer = new byte[512];
                int primljeno = udpSoket.ReceiveFrom(bafer, ref remoteEp);

                string odgovor = Encoding.UTF8.GetString(bafer, 0, primljeno).Trim();
                var delovi = odgovor.Split(';');

                if (delovi[0] == "OK")
                {
                    int brStola = int.Parse(delovi[1]);
                    ConsoleUI.Info($"✅ Rezervacija potvrđena! Gosti su smešteni na sto {brStola}.");
                    ConsoleUI.Info($"Sto je sada ZAUZET i možete primiti porudžbinu.");
                }
                else
                {
                    ConsoleUI.Error($"❌ {delovi[1]}");
                }

                udpSoket.Close();
                ConsoleUI.Pause();
            }
            catch (Exception ex)
            {
                ConsoleUI.Error(ex.Message);
                ConsoleUI.Pause();
            }
        }
    }
}