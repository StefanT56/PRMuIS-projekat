using Server.Handler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Networking
{
    public sealed class ClientSession
    {
        private readonly int _clientId;
        private readonly TcpClient _client;
        private readonly IMessageHandler _handler;

        public ClientSession(int clientId, TcpClient clientSecret, IMessageHandler handler)
        {
            _clientId = clientId;
            _client = clientSecret;
            _handler = handler;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            try
            {
                using (_client)
                using (NetworkStream ns = _client.GetStream())
                {
                    string hello = await MessageFramer.ReadFrameAsync(ns, ct).ConfigureAwait(false);
                    if (hello == null) return;

                    if (!hello.StartsWith("HELLO|", StringComparison.OrdinalIgnoreCase) ||
                        hello.IndexOf("role=WAITER", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        await MessageFramer.WriteFrameAsync(ns, "ERR|msg=Ocekivana poruka HELLO sa ulogom KONOBAR", ct).ConfigureAwait(false);
                        return;
                    }

                    await MessageFramer.WriteFrameAsync(ns, $"OK|msg=Konekcija uspesno uspostavljena|clientId={_clientId}", ct).ConfigureAwait(false);

                    // loop
                    while (!ct.IsCancellationRequested)
                    {
                        string msg = await MessageFramer.ReadFrameAsync(ns, ct).ConfigureAwait(false);
                        if (msg == null) break;

                        string resp = _handler.Handle(_clientId, msg);
                        await MessageFramer.WriteFrameAsync(ns, resp, ct).ConfigureAwait(false);
                    }
                }
            }
            catch (IOException)
            {
                // disconnect
            }
            catch (ObjectDisposedException)
            {
                // ok
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SESSION #{_clientId}] Greška: {ex.Message}");
            }
        }

    }
}
