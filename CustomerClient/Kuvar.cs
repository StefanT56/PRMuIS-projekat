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

namespace CustomerClient
{
    class Kuvar
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                ConsoleUI.Error("Pokretanje Kuvar.exe nije uspelo");
                ConsoleUI.Pause();
                return;
            }

            int kuvarId = int.Parse(args[0]);
            int count = int.Parse(args[1]);
            int udpPort = int.Parse(args[2]);

            ConsoleUI.Clear();
            ConsoleUI.Header("Kuvar");
            ConsoleUI.Info($"Kuvar id#{kuvarId}, UDP port {udpPort}");

            const string serverIp = "127.0.0.1";
            const int serverPort = 5000;

            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);  //TACKA 5

            int pokusaji = 0;
            while (true)
            {
                try
                {
                    ConsoleUI.Info("Pokušavam da se konektujem sa serverom...");
                    sock.Connect(new IPEndPoint(IPAddress.Parse(serverIp), serverPort));    //TACKA 5
                    ConsoleUI.Info("Konektovan na server.");
                    break;
                }
                catch (SocketException e)
                {
                    ConsoleUI.Warning($"Greška pri konekciji: {e.Message}");

                    pokusaji++;
                    if (pokusaji >= 5)
                    {
                        ConsoleUI.Error("Previše neuspešnih pokušaja konekcije. Gasim klijenta.");
                        ConsoleUI.Pause();
                        sock.Close();
                        return;
                    }

                    Thread.Sleep(200);
                }
            }

            string regMsg = $"REGISTER;{kuvarId};Kuvar;{udpPort}\n";        //TACKA------------------------- 5

            try
            {
                sock.Send(Encoding.UTF8.GetBytes(regMsg));
            }
            catch (SocketException ex)
            {
                ConsoleUI.Error(ex.Message);
                ConsoleUI.Pause();
                sock.Close();
                return;
            }

            var ackBuf = new byte[8192];
            int bytesRecvd;

            try
            {
                bytesRecvd = sock.Receive(ackBuf);
            }
            catch (SocketException ex)
            {
                ConsoleUI.Error(ex.Message);
                ConsoleUI.Pause();
                sock.Close();
                return;
            }

            string ack = Encoding.UTF8.GetString(ackBuf, 0, bytesRecvd).Trim();

            if (ack != "OK")
            {
                ConsoleUI.Error($"Registracija neuspešna: {ack}");
                ConsoleUI.Pause();
                sock.Close();
                return;
            }                        //--------------------------------TACKA 5

            ConsoleUI.Info("Uspešno registrovan, čekam porudžbine...");

            Socket orderSock = null;

            try
            {
                Thread.Sleep(5000);

                orderSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Thread.Sleep(10000);
                orderSock.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5001));
            }
            catch (Exception ex)
            {
                ConsoleUI.Error($"Ne mogu da se povežem na ready port: {ex.Message}");
                ConsoleUI.Pause();
                sock.Close();
                try { orderSock?.Close(); } catch { }
                return;
            }

            while (true)   //-----prijem i obrada porudzbina TACKA 5
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
                string b64 = parts[3];   //KUVAR PRIMA PORUDZBINE 
                byte[] orderData = Convert.FromBase64String(b64);


                Sto sto = RepozitorijumStolova.DobaviPoId(brStola);

                List<Porudzbina> porudzbine;
                using (var ms = new MemoryStream(orderData))
                {
                    var bf = new BinaryFormatter();
                    porudzbine = (List<Porudzbina>)bf.Deserialize(ms);
                }

                ConsoleUI.Clear();
                ConsoleUI.Header("Kuvar - zadatak");
                ConsoleUI.Info($"Porudžbina za sto {brStola} od konobara {konobar}");
                ConsoleUI.Info($"Naručeno stavki: {porudzbine.Count}");

                foreach (Porudzbina p in porudzbine)
                {
                    ConsoleUI.Info(p.ToString());
                }

                var rnd = new Random();

                foreach (Porudzbina p in porudzbine)
                {
                    p.statusArtikla = StatusArtikla.priprema;
                    ConsoleUI.Info($"Priprema u toku: {p.nazivArtikla}");

                    Thread.Sleep(rnd.Next(1000, 3000));

                    p.statusArtikla = StatusArtikla.spremno;
                    ConsoleUI.Info($"Priprema gotova: {p.nazivArtikla}");
                }

                Thread.Sleep(2000);
                ConsoleUI.Info($"Porudžbina kompletirana | STO:{brStola} | KONOBAR:{konobar}");

                // NAPOMENA: imaš "hrane" u poruci (tipfeler). Ako server očekuje "hrana", promeni.
                string readyMsg = $"NOTIFY_READY;{brStola};{konobar};hrane\n";
                ConsoleUI.Info("Server obavešten o završetku porudžbine.");

                orderSock.Send(Encoding.UTF8.GetBytes(readyMsg));

                ConsoleUI.Pause();
            }

            try { sock.Close(); } catch { }
            try { orderSock?.Close(); } catch { }
        }
    }
}