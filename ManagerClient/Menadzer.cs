using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Application.Services;
using Application.UI;
using Core.Models;
using Infrastructure.Repositories;

namespace ManagerClient
{
    public class Menadzer
    {
        public static void Main(string[] args)
        {


            try
            {

                int.TryParse(args.Length > 0 ? args[0] : "0", out int idMenadzera);
                int.TryParse(args.Length > 1 ? args[1] : "1", out int brojMenadzera);
                int.TryParse(args.Length > 2 ? args[2] : "4010", out int udpLuka);

                ConsoleUI.Clear();
                ConsoleUI.Header("Menadzer");


                Console.WriteLine("ARGS: " + string.Join(", ", args));

                ConsoleUI.Info($"Menadzer id={idMenadzera}, broj={brojMenadzera}, udpPort={udpLuka}");


                var tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                tcpSocket.Connect(new IPEndPoint(IPAddress.Loopback, 5000));

                string regMsg = $"REGISTER;{idMenadzera};Menadzer;{udpLuka}\n";
                tcpSocket.Send(Encoding.UTF8.GetBytes(regMsg));

                byte[] ackbytes = new byte[1024];
                int bytesRecieved = tcpSocket.Receive(ackbytes);
                string ack = Encoding.UTF8.GetString(ackbytes, 0, bytesRecieved).Trim();

                if (ack != "OK")
                {
                    ConsoleUI.Error($"REGISTRACIJA NEUSPESNA: {ack}");
                    ConsoleUI.Pause();
                    return;
                }

                ConsoleUI.Info("Uspesno registrovan.");


                var repoMenadzera = new RepozitorijumMenadzera(2);
                var servisUpravljanja = new ServisUpravljanjaMenadzerom(repoMenadzera);

                var menuThread = new Thread(() =>
                {
                    try
                    {
                        servisUpravljanja.ZaumiIliRezervistiSto(brojMenadzera, TipOsoblja.Menadzer);
                    }
                    catch (Exception ex)
                    {
                        ConsoleUI.Error("[MENU THREAD] " + ex.Message);
                        ConsoleUI.Pause();
                    }
                })
                { IsBackground = false };
                menuThread.Start();


                var servisOslobadjanja = new ServisOslobadjanjaStolovaMenadzer(repoMenadzera, 4001);
                var releaseThread = new Thread(() =>
                {
                    try
                    {
                        servisOslobadjanja.OslobodiSto(brojMenadzera);
                    }
                    catch (Exception ex)
                    {
                        ConsoleUI.Error("[RELEASE THREAD] " + ex.Message);
                    }
                })
                { IsBackground = true };
                releaseThread.Start();


                var udpThread = new Thread(() =>
                {
                    Socket udpSocket = null;

                    try
                    {
                        udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);


                        udpSocket.Bind(new IPEndPoint(IPAddress.Any, udpLuka));

                        EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                        byte[] buffer = new byte[1024];

                        while (true)
                        {
                            int received = udpSocket.ReceiveFrom(buffer, ref remote);
                            if (received <= 0) continue;

                            string message = Encoding.UTF8.GetString(buffer, 0, received).Trim();

                            if (message.StartsWith("RESERVATION_USED;"))
                            {
                                var parts = message.Split(';');
                                if (parts.Length == 2 && int.TryParse(parts[1], out int idRez))
                                {
                                    ConsoleUI.Info($"Rezervacija #{idRez} je iskorišćena.");
                                    repoMenadzera.UkloniRezervaciju(idRez);
                                }
                                else
                                {
                                    ConsoleUI.Warning($"Neispravna poruka: {message}");
                                }
                            }
                            else
                            {
                                ConsoleUI.Warning($"Nepoznata poruka: {message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleUI.Error("[UDP THREAD] " + ex.Message);
                        ConsoleUI.Pause();
                    }
                    finally
                    {
                        try { udpSocket?.Close(); } catch { }
                    }
                })
                { IsBackground = true };
                udpThread.Start();


                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                ConsoleUI.Error("[MAIN CATCH] " + ex);
                ConsoleUI.Pause();
            }
        }
    }
}