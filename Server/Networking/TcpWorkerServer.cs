using Domain.Enumi;
using Domain.Modeli;
using Infrastructure.Servisi;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Networking
{
    public class TcpWorkerServer
    {
        private readonly int _port;
        private readonly ServerState _state;
        private TcpListener _listener;

        // workerId -> session
        private readonly ConcurrentDictionary<int, WorkerSession> _sessions =
            new ConcurrentDictionary<int, WorkerSession>();

        public TcpWorkerServer(int port, ServerState state)
        {
            _port = port;
            _state = state;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            Console.WriteLine("TCP Worker server sluša na portu " + _port);

            while (!ct.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                _ = Task.Run(() => HandleClientAsync(client, ct));
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            var session = new WorkerSession(client, _state, this);

            try
            {
                await session.RunAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Worker session error: " + ex.Message);
            }
        }

        internal void RegisterSession(int workerId, WorkerSession session)
        {
            _sessions[workerId] = session;
        }

        internal void UnregisterSession(int workerId)
        {
            WorkerSession s;
            _sessions.TryRemove(workerId, out s);
        }

        public bool TrySendAssign(int workerId, int itemId, OrderItem item)
        {
            WorkerSession s;
            if (!_sessions.TryGetValue(workerId, out s))
                return false;

            return s.TrySendAssign(itemId, item);
        }
    }
    internal class WorkerSession
    {
        private readonly TcpClient _client;
        private readonly ServerState _state;
        private readonly TcpWorkerServer _server;

        private int _workerId;
        private ClientRole _role;

        private StreamReader _reader;
        private StreamWriter _writer;

        public WorkerSession(TcpClient client, ServerState state, TcpWorkerServer server)
        {
            _client = client;
            _state = state;
            _server = server;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            using (_client)
            using (var ns = _client.GetStream())
            using (_reader = new StreamReader(ns, Encoding.UTF8))
            using (_writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true })
            {
                // HELLO|id=1|role=Kuvar
                var hello = await _reader.ReadLineAsync().ConfigureAwait(false);
                if (!TryParseHello(hello, out _workerId, out _role))
                {
                    await _writer.WriteLineAsync("ERR|bad_hello").ConfigureAwait(false);
                    return;
                }

                // upiši resurs u state
                _state.RegisterOrUpdateResource(new ClientInfo { Id = _workerId, role = _role, zauzet = false });

                _server.RegisterSession(_workerId, this);
                await _writer.WriteLineAsync("OK|HELLO").ConfigureAwait(false);

                while (!ct.IsCancellationRequested)
                {
                    var line = await _reader.ReadLineAsync().ConfigureAwait(false);
                    if (line == null) break;

                    // ITEM_DONE|workerId=1|itemId=123
                    if (line.StartsWith("ITEM_DONE|"))
                    {
                        int itemId;
                        if (TryGetInt(line, "itemId", out itemId))
                        {
                            _state.SetItemStatus(itemId, OrderStatus.Spremno);
                            _state.ReleaseResource(_workerId);

                            await _writer.WriteLineAsync("OK|DONE").ConfigureAwait(false);
                        }
                        else
                        {
                            await _writer.WriteLineAsync("ERR|bad_itemId").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await _writer.WriteLineAsync("ERR|unknown").ConfigureAwait(false);
                    }
                }
            }

            // disconnect cleanup
            _server.UnregisterSession(_workerId);
            _state.ReleaseResource(_workerId);
        }

        public bool TrySendAssign(int itemId, OrderItem item)
        {
            try
            {
                // Minimalno šaljemo ID + tip + sto
                _writer.WriteLine("ITEM_ASSIGN|itemId=" + itemId + "|table=" + item.BrojStola + "|kat=" + item.Kategorija);
                return true;
            }
            catch { return false; }
        }

        private static bool TryParseHello(string line, out int id, out ClientRole role)
        {
            id = 0;
            role = ClientRole.Kuvar;

            if (string.IsNullOrWhiteSpace(line)) return false;
            if (!line.StartsWith("HELLO|")) return false;

            int tmpId;
            if (!TryGetInt(line, "id", out tmpId)) return false;

            string roleStr;
            if (!TryGetString(line, "role", out roleStr)) return false;

            ClientRole tmpRole;
            if (!Enum.TryParse(roleStr, out tmpRole)) return false;

            id = tmpId;
            role = tmpRole;
            return true;
        }

        private static bool TryGetInt(string line, string key, out int value)
        {
            value = 0;
            string s;
            if (!TryGetString(line, key, out s)) return false;
            return int.TryParse(s, out value);
        }

        private static bool TryGetString(string line, string key, out string value)
        {
            value = null;
            var parts = line.Split('|');
            foreach (var p in parts)
            {
                var kv = p.Split('=');
                if (kv.Length == 2 && kv[0] == key)
                {
                    value = kv[1];
                    return true;
                }
            }
            return false;
        }
    }
}
