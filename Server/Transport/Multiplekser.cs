using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server.Transport
{
    /// <summary>
    /// Multiplekser koristi Socket.Select() da obrađuje više soketa u JEDNOM thread-u
    /// umjesto da kreira novi thread za svakog klijenta.
    /// Ovo je efikasnije i zadovoljava zahtjev ZADATKA 7 za multipleksiranjem.
    /// </summary>
    public class Multiplekser
    {
        private readonly List<Socket> aktivniTcpSoketi = new List<Socket>();
        private readonly object soketiLock = new object();
        private readonly Action<Socket> obradaHandler;
        private bool radi = true;

        public Multiplekser(Action<Socket> handler)
        {
            obradaHandler = handler;
        }

        public void DodajSoket(Socket soket)
        {
            lock (soketiLock)
            {
                if (!aktivniTcpSoketi.Contains(soket))
                {
                    aktivniTcpSoketi.Add(soket);
                    Console.WriteLine(string.Format("[Multiplekser] Dodat soket (ukupno: {0})", aktivniTcpSoketi.Count));
                }
            }
        }

        public void UkloniSoket(Socket soket)
        {
            lock (soketiLock)
            {
                aktivniTcpSoketi.Remove(soket);
                Console.WriteLine(string.Format("[Multiplekser] Uklonjen soket (ukupno: {0})", aktivniTcpSoketi.Count));
            }
        }

        /// <summary>
        /// Glavni multipleksiranje loop - JEDAN thread za SVE sokete
        /// </summary>
        public void PokreniMultipleksiranje()
        {
            new Thread(() =>
            {
                while (radi)
                {
                    try
                    {
                        List<Socket> citajListu;

                        lock (soketiLock)
                        {
                            citajListu = aktivniTcpSoketi.ToList();
                        }

                        if (citajListu.Count == 0)
                        {
                            Thread.Sleep(100);
                            continue;
                        }

                        // ✅ MULTIPLEKSIRANJE - Socket.Select() provjerava SVE sokete odjednom
                        // Timeout: 1 sekunda (1,000,000 mikrosekundi)
                        Socket.Select(citajListu, null, null, 1000000);

                        // Sada obradimo SAMO sokete koji imaju podatke za čitanje
                        foreach (var soket in citajListu)
                        {
                            try
                            {
                                if (soket.Available > 0)
                                {
                                    obradaHandler(soket);
                                }
                            }
                            catch (SocketException)
                            {
                                // Soket je zatvoren, ukloni ga
                                UkloniSoket(soket);
                                BezbednoZatvori(soket);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(string.Format("[Multiplekser] Greška pri obradi: {0}", ex.Message));
                                UkloniSoket(soket);
                                BezbednoZatvori(soket);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(string.Format("[Multiplekser] Greška u loop-u: {0}", ex.Message));
                        Thread.Sleep(100);
                    }
                }
            })
            { IsBackground = true }.Start();

            Console.WriteLine("[Multiplekser] Pokrenut - koristi Socket.Select() za efikasnu obradu");
        }

        public void Zaustavi()
        {
            radi = false;
        }

        private void BezbednoZatvori(Socket s)
        {
            try { s?.Shutdown(SocketShutdown.Both); } catch { }
            try { s?.Close(); } catch { }
        }
    }
}
