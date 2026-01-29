using Application.HelperUI;
using Application.Servisi;
 
using Domain.Enumi;
using Domain.Servisi;
using Infrastructure.Networking;
using Infrastructure.Serializacija;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Threading.Tasks;

namespace Kuvar
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "KUVAR";

            //infrastruktura 
            INetworkClient networkClient = new TcpNetworkClient();
            IObjectSerializer serializer = new BinaryObjectSerializer();

            if (!Connect(networkClient)) return;

            var handler = new ConsoleMessageHandler();
            var listener = new ClientMessageListener(networkClient, serializer, handler);
            listener.Start();

            var session = new ClientSession(networkClient, serializer);
            var commands = new WorkerCommands(session, ClientRole.Kuvar);

            commands.Register();
            RunMenu(networkClient, commands);
            listener.Stop();
            networkClient.Disconnect();
        }

        private static bool Connect(INetworkClient netClinet)
        {
            Console.Write("IP servera [127.0.0.1]: ");
            string ip = Console.ReadLine();
            if(string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";

            Console.Write("Port [9000]: ");
            string portRaw = Console.ReadLine();
            int port = 9000; // MOGUCI BUG negdje pise port 50001 kao sa vjezbi ako bude doslo do problema ovdje pogledati 
            if (!string.IsNullOrWhiteSpace(portRaw))
                int.TryParse(portRaw, out port);

            if (port <= 0)
                port = 9000; // za - portove 

            netClinet.Connect(ip, port);
            if(!netClinet.IsConnected)
            {
                UIHelper.DrawError("Ne povezuje na server!");
                UIHelper.Pause();
                return false;
            }
            UIHelper.DrawInfo("Uspjesno povezan na server!");
            UIHelper.Pause();
            return true;
        }
        private static void RunMenu(INetworkClient netClient ,WorkerCommands commands)
        {
            while (netClient.IsConnected)
            {
                UIHelper.DrawHeader("KUVAR – RADNA KONZOLA");

                UIHelper.DrawOpition("1", "Označi zadatak kao GOTOV");
                UIHelper.DrawOpition("0", "Izlaz");

                Console.Write("\n Izbor: ");
                string izbor = Console.ReadLine();

                if (izbor == "0")
                    break;
                if(izbor == "1")
                {
                    Console.Write("\n Unesi TaksID: ");
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
