using Domain.Enumi;
using Domain.Poruke;
using Domain.Servisi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Application.Servisi
{
    public class ConsoleMessageHandler : IMessageHandler
    {
        public void Handle(NetworkMessage poruka)
        {
            if (poruka == null) return;

            switch (poruka.type)
            {
                case MessageType.TaskAssigned:
                    HandleTaskAssigned(poruka.Data);
                    break;
                case MessageType.OrderReady:
                    HandleOrderReady(poruka.Data);
                    break;
                case MessageType.Error:
                    HandleError(poruka.Data);
                    break;
                default:
                    Console.WriteLine("[INFO] Primjeljna poruka : " + poruka.type);
                    break;

            }


        }
        private void HandleTaskAssigned(object data)
        {
            TaskAssignedMessage payload = data as TaskAssignedMessage;
            if(payload == null)
            {
                Console.WriteLine("[WARN] TaskAssigned je nevalidan .");
                return;
            }
            string itemText = " ";
            if(payload.stavka != null)
            {
                itemText = payload.stavka.Naziv + " x " + payload.stavka.cena + " [" + payload.stavka.Kategorija + "] ";
            }
            Console.WriteLine("[TASK] Assigned: TaskId=" + payload.TaskId +", Sto=" + payload.BrojStola +", Item=" + itemText);
        }
        private void HandleOrderReady(object data)
        {
            OrderReadyMessage payload = data as OrderReadyMessage;
            if (payload == null)
            {
                Console.WriteLine("[WARN] OrderReady payload prazan.");
                return;
            }

            Console.WriteLine("[READY] OrderId=" + payload.OrderId +" je spremno za Sto=" + payload.BrojStola);
        }

        private void HandleError(object data)
        {
            ErrorMessage payload = data as ErrorMessage;
            if (payload == null)
            {
                Console.WriteLine("[ERROR] Pejload je prazan.");
                return;
            }

            string msg = payload.Poruka ?? "(Nema prouke)";
            Console.WriteLine("[ERROR] " + msg);
        }

    }
}
