using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    internal static class MrezniSlusaoci
    {
        /// <summary>
        /// ORIGINALNA implementacija - RADI GARANTOVANO
        /// Kreira novi thread za svakog klijenta
        /// </summary>
        public static void PokreniTcpSlusalac(int port, Action<Socket> obradaKlijenta)
        {
            new Thread(() =>
            {
                Socket slusalac = null;
                try
                {
                    slusalac = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    slusalac.Bind(new IPEndPoint(IPAddress.Any, port));
                    slusalac.Listen(100);
                    Console.WriteLine(string.Format("[TCP:{0}] Slusalac pokrenut", port));

                    while (true)
                    {
                        var klijent = slusalac.Accept();
                        Console.WriteLine(string.Format("[TCP:{0}] Novi klijent povezan", port));

                        //   MULTIPLEKSIRANJE - koristi Socket.Poll() za provjeru podataka
                        new Thread(() =>
                        {
                            try
                            {

                                while (true)
                                {
                                    // Poll() provjerava da li soket ima podatke
                                    // Timeout: 100ms
                                    if (klijent.Poll(100000, SelectMode.SelectRead))
                                    {
                                        if (klijent.Available > 0)
                                        {
                                            // Ima podatke, obradi
                                            break;
                                        }
                                        else
                                        {
                                            // Soket je zatvoren
                                            return;
                                        }
                                    }
                                }

                                obradaKlijenta(klijent);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(string.Format("[TCP:{0}] Greska: {1} - {2}", port, ex.GetType().Name, ex.Message));
                            }
                            finally
                            {
                                BezbednoZatvori(klijent);
                            }
                        })
                        { IsBackground = true }.Start();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("[TCP:{0}] Neuspjelo pokretanje: {1}", port, ex.Message));
                    BezbednoZatvori(slusalac);
                }
            })
            { IsBackground = true }.Start();
        }

        public static void PokreniUdpSlusalac(int port, Action<string, EndPoint, Socket> obradaPoruke)
        {
            new Thread(() =>
            {
                Socket udp = null;
                try
                {
                    udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    udp.Bind(new IPEndPoint(IPAddress.Any, port));
                    Console.WriteLine(string.Format("[UDP:{0}] Slusalac pokrenut", port));

                    while (true)
                    {
                        EndPoint udaljeni = new IPEndPoint(IPAddress.Any, 0);
                        byte[] bafer = new byte[8192];

                        int primljeno = udp.ReceiveFrom(bafer, ref udaljeni);
                        if (primljeno <= 0) continue;

                        string poruka = Encoding.UTF8.GetString(bafer, 0, primljeno).Trim();
                        obradaPoruke(poruka, udaljeni, udp);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("[UDP:{0}] Neuspjelo pokretanje: {1}", port, ex.Message));
                    BezbednoZatvori(udp);
                }
            })
            { IsBackground = true }.Start();
        }

        public static string ProcitajTekst(Socket klijent, int max = 8192)
        {
            var bafer = new byte[max];
            int primljeno = klijent.Receive(bafer);
            if (primljeno <= 0) return string.Empty;
            return Encoding.UTF8.GetString(bafer, 0, primljeno).Trim();
        }

        public static void PosaljiUdp(Socket udp, EndPoint udaljeni, string tekst)
        {
            byte[] podaci = Encoding.UTF8.GetBytes(tekst);
            udp.SendTo(podaci, udaljeni);
        }

        public static void BezbednoZatvori(Socket s)
        {
            try { s?.Shutdown(SocketShutdown.Both); } catch { }
            try { s?.Close(); } catch { }
        }
    }
}
