using Application.Servisi;
using Domain.Servisi;
using Infrastructure.Networking;
using Infrastructure.Serializacija;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace PRMUIS
{
    internal class Program
    {
        static void Main(string[] args)
        {
            INetworkClient networkClient = new TcpNetworkClient();
            IObjectSerializer serializer = new BinaryObjectSerializer();

            // 2) Application
            IMessageHandler handler = new ConsoleMessageHandler();
            ClientMessageListener listener =
                new ClientMessageListener(networkClient, serializer, handler);

            // 3) Connect
            networkClient.Connect("127.0.0.1", 9000);

            // 4) Start listening
            listener.Start();

            Console.WriteLine("Klijent pokrenut. Pritisni ENTER za izlaz...");
            Console.ReadLine();

            // 5) Cleanup
            listener.Stop();
            networkClient.Disconnect();
        }
    }
}
