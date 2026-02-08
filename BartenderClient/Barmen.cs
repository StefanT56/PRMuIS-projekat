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

namespace BartenderClient
{
    class Barmen
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                ConsoleUI.Error("Pokretanje Barmen.exe nije uspelo");
                ConsoleUI.Pause();
                return;
            }

            int barmenId = int.Parse(args[0]);
            int count = int.Parse(args[1]);
            int udpPort = int.Parse(args[2]);

            ConsoleUI.Clear();
            ConsoleUI.Header("Barmen");
            ConsoleUI.Info($"Šanker id#{barmenId}, UDP port {udpPort}");

            const string serverIp = "127.0.0.1";
            const int registerPort = 5000;
            const int readyPort = 5001;

            Socket sock = null;
            Socket orderSock = null;

            try  // tacka 5 barmen prima porudzbine od servera preko tcpa 
            {
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sock.Connect(new IPEndPoint(IPAddress.Parse(serverIp), registerPort));

                string regMsg = $"REGISTER;{barmenId};Barmen;{udpPort}\n";
                sock.Send(Encoding.UTF8.GetBytes(regMsg));

                var ackBuf = new byte[8192];
                int bytesRecvd = sock.Receive(ackBuf);
                string ack = Encoding.UTF8.GetString(ackBuf, 0, bytesRecvd).Trim();

                if (ack != "OK")
                {
                    ConsoleUI.Error($"Registracija neuspešna: {ack}");
                    ConsoleUI.Pause();
                    sock.Close();
                    return;
                }

                ConsoleUI.Info("Uspešno registrovan, čekam narudžbine...");


                Thread.Sleep(5000);

                orderSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Thread.Sleep(10000);
                orderSock.Connect(new IPEndPoint(IPAddress.Parse(serverIp), readyPort));

                while (true)
                {
                    bytesRecvd = sock.Receive(ackBuf);
                    if (bytesRecvd <= 0)
                    {
                        ConsoleUI.Warning("Veza je prekinuta od strane servera.");
                        break;
                    }

                    string msg = Encoding.UTF8.GetString(ackBuf, 0, bytesRecvd).Trim();

                    if (!msg.StartsWith("PREPARE;"))
                        continue;

                    var parts = msg.Split(new[] { ';' }, 4);
                    int brStola = int.Parse(parts[1]);
                    int konobar = int.Parse(parts[2]);
                    string b64 = parts[3];

                    byte[] orderData = Convert.FromBase64String(b64);  //BARMEN PRIMA PODATKE TACKA 3
                    List<Porudzbina> porudzbine;

                    using (var ms = new MemoryStream(orderData))
                    {
                        var bf = new BinaryFormatter();
                        porudzbine = (List<Porudzbina>)bf.Deserialize(ms);
                    }

                    ConsoleUI.Clear();
                    ConsoleUI.Header("Barmen - zadatak");
                    ConsoleUI.Info($"Porudžbina za sto {brStola} od konobara {konobar}:");

                    foreach (Porudzbina p in porudzbine)
                    {
                        ConsoleUI.Info(p.ToString());
                    }

                    foreach (var p in porudzbine)
                    {
                        p.statusArtikla = StatusArtikla.priprema;
                        ConsoleUI.Info($"Priprema u toku: {p.nazivArtikla}");

                        Thread.Sleep(new Random().Next(500, 2000));

                        p.statusArtikla = StatusArtikla.spremno;
                        ConsoleUI.Info($"Priprema gotova: {p.nazivArtikla}");
                    }

                    ConsoleUI.Info($"Narudžbina kompletirana | STO:{brStola} | KONOBAR:{konobar}");

                    string readyMsg = $"NOTIFY_READY;{brStola};{konobar};pice\n";
                    orderSock.Send(Encoding.UTF8.GetBytes(readyMsg));

                    ConsoleUI.Info("Server obavešten o završetku porudžbine.");
                    ConsoleUI.Pause();
                }
            }
            catch (Exception ex)
            {
                ConsoleUI.Error(ex.Message);
                ConsoleUI.Pause();
            }
            finally
            {
                try { sock?.Close(); } catch { }
                try { orderSock?.Close(); } catch { }
            }
        }
    }
}