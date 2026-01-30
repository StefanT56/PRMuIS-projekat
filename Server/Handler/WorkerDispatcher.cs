using Domain.Enumi;
using Domain.Modeli;
using Infrastructure.Servisi;
using Server.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Handler
{
    public class WorkerDispatcher
    {
        private readonly ServerState _state;
        private readonly TcpWorkerServer _workerServer;

        public WorkerDispatcher(ServerState state, TcpWorkerServer workerServer)
        {
            _state = state;
            _workerServer = workerServer;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                _state.CleanupExpiredReservations(DateTime.Now);

                // dodela kuvaru
                DispatchForRole(ClientRole.Kuvar);

                // dodela barmenu
                DispatchForRole(ClientRole.Barmen);

                await Task.Delay(100, ct).ConfigureAwait(false);
            }
        }

        private void DispatchForRole(ClientRole role)
        {
            // pokušaj da nađeš slobodan resurs tog role-a
            ClientInfo res;
            if (!_state.TryAcquireResource(role, out res))
                return;

            // uzmi sledeću stavku
            OrderItem item;
            if (!_state.TryDequeueNextFor(role, out item))
            {
                // nema posla -> oslobodi resurs
                _state.ReleaseResource(res.Id);
                return;
            }

            // registruj item da dobije ID
            int itemId = _state.RegisterItem(item);

            // pošalji worker-u preko TCP
            bool sent = _workerServer.TrySendAssign(res.Id, itemId, item);
            if (!sent)
            {
                // ako ne može da pošalje, vrati item nazad u čekanje i oslobodi resurs
                _state.ReleaseResource(res.Id);
                _state.PushWaiting(item, item.Kategorija);
            }
            else
            {
                 _state.SetItemStatus(itemId, OrderStatus.UPripremi);
            }
        }
    }
}
