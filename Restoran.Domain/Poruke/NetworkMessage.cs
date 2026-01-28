using Domain.Enumi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Poruke
{
    [Serializable] 
    public class NetworkMessage
    {
        public MessageType type;
        public object Data;
    }
}
