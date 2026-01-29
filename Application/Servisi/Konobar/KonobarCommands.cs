using Domain.Enumi;
using Domain.Modeli;
using Domain.Poruke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Servisi.Konobar
{
    public class KonobarCommands
    {
        private readonly ClientSession _session;
        
        public KonobarCommands(ClientSession session)
        {
            _session = session; 
        }
        public void Register()
        {
            _session.Send(MessageType.RegisterClient, new RegisterClientRequest { Role = ClientRole.Konobar });
        }
        public void TaskDone(int takdId)
        {
            if (takdId <= 0) return;
            _session.Send(MessageType.TaskDone, new TaskDoneRequest { TaskId = takdId });
        }
        public void CreateOrder(int brojStola,List<OrderItem> stavke)
        {
            return;
        }
    }

}
