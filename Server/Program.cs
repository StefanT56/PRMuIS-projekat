using Application.Servisi;
using Domain.Modeli;
using Infrastructure.Servisi;
using Server.Handler;
using Server.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class Program
    {
        static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();

            // inicijalni stolovi (primer)
            var tables = new List<Table>
            {
                new Table (1, 4),
                new Table (2, 4),
                new Table (3, 6)
            };

            var state = new ServerState(tables);


            // TCP server za kuvara/barmena (worker-e)
            var workerServer = new TcpWorkerServer(5001, state);
            Task.Run(() => workerServer.RunAsync(cts.Token));

            // dispatcher loop: dodeljuje stavke slobodnim resursima
            var dispatcher = new WorkerDispatcher(state, workerServer);
            Task.Run(() => dispatcher.RunAsync(cts.Token));


            //TCP server za konobara
            var registry = new UdpRoleRegistry();
            var notifier = new UdpNotifier(registry);
            var handler = new WaiterMessageHandler(state, notifier);
            var tcpServer = new TcpWaiterServer(5000, handler);

            Task.Run(() => tcpServer.RunAsync(cts.Token));

            Console.WriteLine("Server radi. ENTER za stop.");
            Console.ReadLine();
            cts.Cancel();
        }
    }
}
