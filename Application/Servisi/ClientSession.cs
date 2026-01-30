using Domain.Enumi;
using Domain.Poruke;
using Domain.Servisi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace Application.Servisi
{
    public class ClientSession
    {
        private readonly INetworkClient _net;
        private readonly IObjectSerializer _ser;

        public ClientSession(INetworkClient net,IObjectSerializer ser)
        {
            _net = net;
            _ser = ser;
        }
        public bool IsConnected
        {
            get { return _net.IsConnected; }
        }

        public void Send(MessageType tip,object obj)
        {
            if (obj == null) return;
            NetworkMessage msg = new NetworkMessage
            {
                type = tip,
                Data = obj
            };
            byte[] bytes = _ser.Serialize(msg);
            _net.Send(bytes);
        }
    }
}
