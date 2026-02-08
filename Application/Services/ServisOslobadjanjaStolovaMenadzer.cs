using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Core.Repositories;

namespace Application.Services
{
    public class ServisOslobadjanjaStolovaMenadzer
    {
        private readonly IRepozitorijumMenadzera repozitorijumMenadzera;
        private readonly int lukaOslobadjanja;

        public ServisOslobadjanjaStolovaMenadzer(IRepozitorijumMenadzera repo, int luka)
        {
            repozitorijumMenadzera = repo;
            lukaOslobadjanja = luka;
        }

        public void OslobodiSto(int brojMenadzera)
        {
            new Thread(() =>
            {
                while (true)
                {
                    repozitorijumMenadzera.PostaviStanjeMenadzera(brojMenadzera, true);
                    var istekle = repozitorijumMenadzera.DobaviIstekleRezervacije();

                    foreach (var isteklaRez in istekle)
                    {
                        using (var klijent = new UdpClient())
                        {
                            string poruka = $"CANCEL_RESERVATION;{isteklaRez.Sto.BrojStola}";
                            byte[] podaci = Encoding.UTF8.GetBytes(poruka);
                            klijent.Send(podaci, podaci.Length, "127.0.0.1", lukaOslobadjanja);

                            klijent.Client.ReceiveTimeout = 1000;
                            try
                            {
                                var ep = new IPEndPoint(IPAddress.Any, 0);
                                var odgovor = klijent.Receive(ref ep);
                                string reply = Encoding.UTF8.GetString(odgovor);

                                repozitorijumMenadzera.UkloniRezervaciju(isteklaRez.Key);
                                Console.WriteLine($"\n[Brisanje rezervacija] Rezervacija #{isteklaRez.Key} za sto {isteklaRez.Key} je istekla i obrisana je.");
                            }
                            catch { }
                        }
                    }

                    repozitorijumMenadzera.PostaviStanjeMenadzera(brojMenadzera, false);
                    Thread.Sleep(10000);
                }
            })
            { IsBackground = true }.Start();
        }
    }
}
