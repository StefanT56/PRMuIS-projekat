using Application.Servisi;

using Application.HelperUI;
using Domain.Enumi;
using Domain.Modeli;
using Domain.Servisi;
using Infrastructure.Networking;
using Infrastructure.Serializacija;
using System;
using System.Collections.Generic;
using System.Net.Configuration;
using Application.Servisi.Konobar;

namespace Konobar
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "KONOBAR";
            INetworkClient networkClient = new TcpNetworkClient();
            IObjectSerializer serializer = new BinaryObjectSerializer();

            if (!Connect(networkClient)) return;

            var handler = new ConsoleMessageHandler();
            var listener = new ClientMessageListener(networkClient, serializer, handler);
            listener.Start();

            var session = new ClientSession(networkClient, serializer);
            var commands = new KonobarCommands(session);

            commands.Register();
            RunMenu(networkClient, commands);

            listener.Stop();
            networkClient.Disconnect();

        }
    private static bool Connect(INetworkClient netClient)
        {
            Console.Write("IP servera [127.0.0.1]: ");
            string ip = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";

            Console.WriteLine("Port [9000]: ");
            string portRaw = Console.ReadLine();
            int port = 9000;
            if (!string.IsNullOrWhiteSpace(portRaw))
                int.TryParse(portRaw, out port);

            if (port <= 0) port = 9000;

            netClient.Connect(ip, port);
            if(!netClient.IsConnected)
            {
                UIHelper.DrawError("Ne moze da se poveze sa serverom!");
                UIHelper.Pause();
                return false;
            }
            UIHelper.DrawInfo("Uspjesno povezan na server!");
            UIHelper.Pause();
            return true;


        }
        private static void RunMenu(INetworkClient networkClient, KonobarCommands commands)
        {
            while (networkClient.IsConnected)
            {
                UIHelper.DrawHeader("KONOBAR – OPERATIVNI MENI");

                UIHelper.DrawOpition("1", "Kreiraj porudžbinu");
                UIHelper.DrawOpition("0", "Izlaz");

                Console.Write("\n Izbor: ");
                string izbor = Console.ReadLine();

                if (izbor == "0")
                    break;

                if (izbor == "1")
                {
                    TokNarudzbe(commands);
                }
                else
                {
                    UIHelper.DrawError("Nepoznata opcija.");
                    UIHelper.Pause();
                }
            }
        }
        private static void TokNarudzbe(KonobarCommands commands)
        {
            UIHelper.DrawError("KONOBAR");
            Console.Write("Broj stola: ");
            if(!int.TryParse(Console.ReadLine(),out int brojStola) || brojStola <= 0)
            {
                UIHelper.DrawError("Neispravan unos broja stola [NUMERACIJA STOLA KRECE OD 1 - N]");
                UIHelper.Pause();
                return;
            }
            List<OrderItem> stavke = new List<OrderItem>();
            while(true)
            {
                Console.WriteLine("\nNaziv stavke [ENTER = kraj]:");
                string naziv = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(naziv)) break;
                Console.Write("Količina: ");
                if (!int.TryParse(Console.ReadLine(), out int kolicina) || kolicina <= 0)
                {
                    UIHelper.DrawError("Neispravna količina.");
                    continue;
                }
                Console.Write("Tip (H=Hrana, P=Piće): ");
                string tipRaw = (Console.ReadLine() ?? "").Trim().ToUpper();
                OrderItemType tip = tipRaw == "P" ? OrderItemType.Pice : OrderItemType.Hrana;
                stavke.Add(new OrderItem
                {
                    Naziv = naziv.Trim(),
                    Kolicina = kolicina,
                    Tip = tip
                });
               UIHelper.DrawInfo("Dodato: " + naziv.Trim() + " x" + kolicina);
            }
            if (stavke.Count == 0)
            {
                UIHelper.DrawError("Nema stavki — porudžbina nije poslata.");
                UIHelper.Pause();
                return;
            }

         //   commands.CreateOrder(brojStola, stavke);

            UIHelper.DrawInfo("Porudžbina poslata (stavki: " + stavke.Count + ").");
            UIHelper.Pause();
        }
    }
}
