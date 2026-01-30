using Server.Handler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Networking
{
    internal class TcpWaiterServer
    {
        private readonly int _port;
        private readonly IMessageHandler _handler;
        private int _nextClientId = 0;

        public TcpWaiterServer(int port, IMessageHandler handler)
        {
            _port = port;
            _handler = handler;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            var listener = new TcpListener(IPAddress.Any, _port);
            listener.Start();
            Console.WriteLine($"[TCP] Waiter server sluša na portu {_port}");

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (!listener.Pending())
                    {
                        await Task.Delay(50, ct).ConfigureAwait(false);
                        continue;
                    }

                    TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    client.NoDelay = true;

                    int clientId = Interlocked.Increment(ref _nextClientId);
                    Console.WriteLine($"[TCP] Konobar povezan #{clientId}: {client.Client.RemoteEndPoint}");

                    var session = new ClientSession(clientId, client, _handler);
                    _ = Task.Run(() => session.RunAsync(ct));
                }
            }
            catch (OperationCanceledException)
            {
                // ok
            }
            finally
            {
                listener.Stop();
            }
        }

    }
}
