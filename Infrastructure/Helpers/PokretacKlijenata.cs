using System;
using System.Diagnostics;
using System.IO;
using Core.Models;
using Core.Repositories;

namespace Infrastructure.Helpers
{
    public class PokretacKlijenata
    {
        private IRepozitorijumKonobara repozitorijumKonobara;
        private IRepozitorijumMenadzera repozitorijumMenadzera;
        private int sledecaLuka = 6000;
        private int sledecaIdKlijenta = 1;

        public void PokreniViseKlijenata(int broj, TipOsoblja tip)
        {
            PokreniProcese(broj, tip);

            if (tip == TipOsoblja.Konobar)
            {
                repozitorijumKonobara = new Repositories.RepozitorijumKonobara();
                for (int i = 0; i < broj; i++)
                {
                    repozitorijumKonobara.DodajKonobara(i + 1);
                }
            }
            else if (tip == TipOsoblja.Menadzer)
            {
                repozitorijumMenadzera = new Repositories.RepozitorijumMenadzera(broj);
            }
        }

        private void PokreniProcese(int broj, TipOsoblja tip)
        {
            string putanjaKlijenta;

            switch (tip)
            {
                case TipOsoblja.Konobar:
                    putanjaKlijenta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "..", "StaffClient", "bin", "Debug", "StaffClient.exe");
                    break;
                case TipOsoblja.Kuvar:
                    putanjaKlijenta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "..", "CustomerClient", "bin", "Debug", "CustomerClient.exe");
                    break;
                case TipOsoblja.Barmen:
                    putanjaKlijenta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "..", "BartenderClient", "bin", "Debug", "BartenderClient.exe");
                    break;
                case TipOsoblja.Menadzer:
                    putanjaKlijenta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "..", "ManagerClient", "bin", "Debug", "ManagerClient.exe");
                    break;
                default:
                    throw new ArgumentException($"Nepoznat tip: {tip}", nameof(tip));
            }

            if (!File.Exists(putanjaKlijenta))
            {
                Console.WriteLine($"Ne mogu pronaći fajl na {putanjaKlijenta}");
                return;
            }

            string radniDir = Path.GetDirectoryName(putanjaKlijenta);
            for (int i = 0; i < broj; i++)
            {
                int idKlijenta = sledecaIdKlijenta++;
                int luka = sledecaLuka++;
                var startInfo = new ProcessStartInfo
                {
                    FileName = putanjaKlijenta,
                    Arguments = $"{idKlijenta} {i + 1} {luka}",
                    WorkingDirectory = radniDir,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
                Console.WriteLine($"Pokrenut klijent #{idKlijenta} kao {tip} na luci {luka}");
            }
        }
    }
}
