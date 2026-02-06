using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Application.UI;
using Infrastructure.Repositories;
using Core.Models;
using Core.Enums;

namespace Server.UI
{
    public static class ServerVizualizacija
    {
        private static bool raditi = true;
        private static Thread vizualizacijaThread;
        private static List<string> poslednjihDogadjaja = new List<string>();
        private static readonly object dogadjajiLock = new object();
        private const int MAX_DOGADJAJA = 8;

        public static void PokreniVizualizaciju()
        {
            vizualizacijaThread = new Thread(() =>
            {
                while (raditi)
                {
                    try
                    {
                        PrikaziKompletnoPregledStanja();
                        Thread.Sleep(10000); // Refresh svake 2 sekunde
                    }
                    catch (Exception ex)
                    {
                        // Tiho ignoriši greške da ne remeti prikaz
                    }
                }
            })
            { IsBackground = true };

            vizualizacijaThread.Start();
        }

        public static void DodajDogadjaj(string poruka)
        {
            lock (dogadjajiLock)
            {
                string vremenskiPecatPoruka = $"[{DateTime.Now:HH:mm:ss}] {poruka}";
                poslednjihDogadjaja.Insert(0, vremenskiPecatPoruka);

                if (poslednjihDogadjaja.Count > MAX_DOGADJAJA)
                {
                    poslednjihDogadjaja.RemoveAt(poslednjihDogadjaja.Count - 1);
                }
            }
        }

        private static void PrikaziKompletnoPregledStanja()
        {
            Console.SetCursorPosition(0, 0);
            Console.Clear();

            var stolovi = RepozitorijumStolova.DobaviSveStolove();

            // ═══════════════════════════════════════════════════════════════════════
            // HEADER
            // ═══════════════════════════════════════════════════════════════════════

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("================================================================================");
            Console.WriteLine("             RESTORANSKI SISTEM - CENTRALNI SERVER                              ");
            Console.WriteLine("================================================================================");
            Console.ResetColor();

            // ═══════════════════════════════════════════════════════════════════════
            // STATISTIKA
            // ═══════════════════════════════════════════════════════════════════════

            int slobodni = stolovi.Count(s => s.Stanje == Status.slobodan);
            int zauzeti = stolovi.Count(s => s.Stanje == Status.zauzet);
            int rezervisani = stolovi.Count(s => s.Stanje == Status.rezervisan);
            int ukupnoPorudzbina = stolovi.Sum(s => s.Porudzbine.Count);

            int porudzbineUPripremi = stolovi.SelectMany(s => s.Porudzbine)
                .Count(p => p.statusArtikla == StatusArtikla.priprema);
            int porudzbineSpremnе = stolovi.SelectMany(s => s.Porudzbine)
                .Count(p => p.statusArtikla == StatusArtikla.spremno);
            int porudzbineDostavljene = stolovi.SelectMany(s => s.Porudzbine)
                .Count(p => p.statusArtikla == StatusArtikla.dostavljeno);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("STATISTIKA:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  Slobodnih stolova:    {slobodni}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  Zauzetih stolova:     {zauzeti}");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  Rezervisanih stolova: {rezervisani}");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  Ukupno porudzbina:    {ukupnoPorudzbina}");
            Console.ResetColor();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("STATUS PORUDZBINA:");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  U pripremi:   {porudzbineUPripremi}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  Spremno:      {porudzbineSpremnе}");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"  Dostavljeno:  {porudzbineDostavljene}");
            Console.ResetColor();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("================================================================================");
            Console.WriteLine("                              PREGLED STOLOVA                                   ");
            Console.WriteLine("================================================================================");
            Console.ResetColor();

            // Tabela zaglavlje
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Sto  | Kapacitet |    Status    | Konobar | Porudzbina |      Detalji        ");
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.ResetColor();

            foreach (var sto in stolovi.OrderBy(s => s.BrojStola))
            {
                PrikaziRedStola(sto);
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("================================================================================");
            Console.ResetColor();

            // ═══════════════════════════════════════════════════════════════════════
            // AKTIVNE PORUDŽBINE (DETALJNO)
            // ═══════════════════════════════════════════════════════════════════════

            var aktivnePorudzbine = stolovi
            .Where(s => s.Porudzbine.Any())
            .SelectMany(s => s.Porudzbine
             .Where(p => p.statusArtikla != StatusArtikla.dostavljeno) // ✅ NE PRIKAZUJ DOSTAVLJENE
             .Select(p => new { Sto = s.BrojStola, Porudzbina = p }))
            .OrderBy(x => x.Porudzbina.statusArtikla)
            .ToList();

            if (aktivnePorudzbine.Any())
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("================================================================================");
                Console.WriteLine("                       AKTIVNE PORUDZBINE - DETALJI                            ");
                Console.WriteLine("================================================================================");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Sto |      Artikal         | Kategorija |     Status     |   Cena   | Konobar");
                Console.WriteLine("--------------------------------------------------------------------------------");
                Console.ResetColor();

                foreach (var item in aktivnePorudzbine)
                {
                    PrikaziRedPorudzbine(item.Sto, item.Porudzbina);
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("================================================================================");
                Console.ResetColor();
            }

            // ═══════════════════════════════════════════════════════════════════════
            // REZERVACIJE
            // ═══════════════════════════════════════════════════════════════════════

            // SEKCIJA ZA REZERVACIJE (dodaj nakon aktivnih porudžbina)
            var rezervisaniStolovi = stolovi.Where(s => s.Stanje == Status.rezervisan).ToList();

            if (rezervisaniStolovi.Any())
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("================================================================================");
                Console.WriteLine("                              AKTIVNE REZERVACIJE                              ");
                Console.WriteLine("================================================================================");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Sto  | Kapacitet | Vreme rezervacije                    ");
                Console.WriteLine("--------------------------------------------------------------------------------");
                Console.ResetColor();

                foreach (var sto in rezervisaniStolovi)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("{0,-4} | ", sto.BrojStola);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("{0,-9} | ", sto.BrojGostiju);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("{0:HH:mm} - {1:HH:mm}", DateTime.Now, DateTime.Now.AddHours(2));
                    Console.ResetColor();
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("================================================================================");
                Console.ResetColor();
            }

            // ═══════════════════════════════════════════════════════════════════════
            // LOG DOGAĐAJA
            // ═══════════════════════════════════════════════════════════════════════

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("================================================================================");
            Console.WriteLine("                            POSLEDNJI DOGADJAJI                                 ");
            Console.WriteLine("================================================================================");
            Console.ResetColor();

            lock (dogadjajiLock)
            {
                if (poslednjihDogadjaja.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("  Nema zabelezenih dogadjaja...");
                    Console.ResetColor();
                }
                else
                {
                    foreach (var dogadjaj in poslednjihDogadjaja)
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine($"  {dogadjaj}");
                        Console.ResetColor();
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("================================================================================");
            Console.ResetColor();

            // ═══════════════════════════════════════════════════════════════════════
            // FOOTER
            // ═══════════════════════════════════════════════════════════════════════

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("Vreme: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{DateTime.Now:HH:mm:ss dd.MM.yyyy}");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("                Server Status: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("ONLINE");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  [Pritisnite ENTER u glavnom prozoru za zaustavljanje servera]");
            Console.ResetColor();
        }

        private static void PrikaziRedStola(Sto sto)
        {
            // Boja prema statusu
            ConsoleColor statusBoja;

            switch (sto.Stanje)
            {
                case Status.slobodan:
                    statusBoja = ConsoleColor.Green;
                    break;

                case Status.zauzet:
                    statusBoja = ConsoleColor.Red;
                    break;

                case Status.rezervisan:
                    statusBoja = ConsoleColor.Yellow;
                    break;

                default:
                    statusBoja = ConsoleColor.Gray;
                    break;
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{sto.BrojStola,-4} | ");
            Console.Write($"{sto.BrojGostiju,-9} | ");

            Console.ForegroundColor = statusBoja;
            Console.Write($"{sto.Stanje.ToString().ToUpper(),-12} | ");
            Console.ResetColor();

            if (sto.ZauzeoKonobar > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"   #{sto.ZauzeoKonobar,-3} ");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("    -    ");
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("| ");
            Console.Write($"{sto.Porudzbine.Count,3} stavki ");

            Console.Write("| ");

            // Detalji o porudžbinama
            if (sto.Porudzbine.Count > 0)
            {
                var upripremi = sto.Porudzbine.Count(p => p.statusArtikla == StatusArtikla.priprema);
                var spremno = sto.Porudzbine.Count(p => p.statusArtikla == StatusArtikla.spremno);
                var dostavljeno = sto.Porudzbine.Count(p => p.statusArtikla == StatusArtikla.dostavljeno);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"Pripr:{upripremi} ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"Sprem:{spremno} ");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write($"Dost:{dostavljeno}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("Nema porudzbina");
            }

            Console.WriteLine();
            Console.ResetColor();
        }

        private static void PrikaziRedPorudzbine(int brojStola, Porudzbina p)
        {
           ConsoleColor statusBoja;

switch (p.statusArtikla)
{
    case StatusArtikla.priprema:
        statusBoja = ConsoleColor.Yellow;
        break;

    case StatusArtikla.spremno:
        statusBoja = ConsoleColor.Green;
        break;

    case StatusArtikla.dostavljeno:
        statusBoja = ConsoleColor.Blue;
        break;

    default:
        statusBoja = ConsoleColor.Gray;
        break;
}

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{brojStola,-3} | ");

            string artikal = p.nazivArtikla.Length > 20 ? p.nazivArtikla.Substring(0, 20) : p.nazivArtikla;
            Console.Write($"{artikal,-20} | ");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{p.KategorijaArtikla,-10} | ");

            Console.ForegroundColor = statusBoja;
            Console.Write($"{p.statusArtikla.ToString().ToUpper(),-14} | ");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{p.cena,7:F2} RSD | ");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"#{p.idKonobara}");

            Console.WriteLine();
            Console.ResetColor();
        }

        public static void Zaustavi()
        {
            raditi = false;
            if (vizualizacijaThread != null && vizualizacijaThread.IsAlive)
            {
                vizualizacijaThread.Join(1000);
            }
        }
    }
}