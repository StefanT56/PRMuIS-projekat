using Application.Servisi;
using Application.HelperUI;
using Domain.Enumi;
using Domain.Servisi;
using Infrastructure.Networking;
using Infrastructure.Serializacija;
using System;

namespace Barmen
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "BARMEN";

            INetworkClient networkClient = new TcpNetworkClient();
            IObjectSerializer serializer = new BinaryObjectSerializer();

            if (!Connect(networkClient))
                return;

            var handler = new ConsoleMessageHandler();
            var listener = new ClientMessageListener(networkClient, serializer, handler);
            listener.Start();

            var session = new ClientSession(networkClient, serializer);
            var commands = new WorkerCommands(session, ClientRole.Barmen);

            commands.Register();
            RunMenu(networkClient, commands);

            listener.Stop();
            networkClient.Disconnect();
        }

        private static bool Connect(INetworkClient networkClient)
        {
            Console.Write("IP servera [127.0.0.1]: ");
            string ip = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(ip))
                ip = "127.0.0.1";

            Console.Write("Port [9000]: ");
            string portRaw = Console.ReadLine();
            int port = 9000;
            if (!string.IsNullOrWhiteSpace(portRaw))
                int.TryParse(portRaw, out port);

            if (port <= 0)
                port = 9000;

            networkClient.Connect(ip, port);

            if (!networkClient.IsConnected)
            {
                UIHelper.DrawError("Ne mogu da se povežem na server.");
                UIHelper.Pause();
                return false;
            }

            UIHelper.DrawInfo("Uspješno povezan na server.");
            UIHelper.Pause();
            return true;
        }

        private static void RunMenu(INetworkClient networkClient, WorkerCommands commands)
        {
            while (networkClient.IsConnected)
            {
                UIHelper.DrawHeader("BARMEN – RADNA KONZOLA");

                UIHelper.DrawOpition("1", "Označi zadatak kao GOTOV");
                UIHelper.DrawOpition("0", "Izlaz");

                Console.Write("\n Izbor: ");
                string izbor = Console.ReadLine();

                if (izbor == "0")
                    break;

                if (izbor == "1")
                {
                    Console.Write("\n Unesi TaskId: ");
                    if (!int.TryParse(Console.ReadLine(), out int taskId) || taskId <= 0)
                    {
                        UIHelper.DrawError("Neispravan TaskId.");
                        UIHelper.Pause();
                        continue;
                    }

                    commands.TaskDone(taskId);
                    UIHelper.DrawInfo($"Zadatak {taskId} označen kao gotov.");
                    UIHelper.Pause();
                }
                else
                {
                    UIHelper.DrawError("Nepoznata opcija.");
                    UIHelper.Pause();
                }
            }
        }
    }
}