using Domain.Enumi;
using Domain.Poruke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Servisi
{
    public class WorkerCommands
    {
        private readonly ClientSession _session;
        private readonly ClientRole _role;

        public WorkerCommands(ClientSession s , ClientRole r)
        {
            _session = s;
            _role = r;
        }
        public void Register()
        {
            _session.Send(MessageType.RegisterClient, new RegisterClientRequest { Role = _role });
        }
        public void TaskDone(int taksID)
        {
            if (taksID <= 0) return;

            _session.Send(MessageType.TaskDone, new TaskDoneRequest { TaskId = taksID });
        }
    }
}
