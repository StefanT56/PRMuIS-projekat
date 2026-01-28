using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Servisi
{
    public interface INetworkClient
    {
        bool IsConnected { get; }
        void Connect(string serverAddress, int port);
        void Disconnect();
        void Send(byte[] data);
        byte[] Receive();
         bool TryReceive(out byte[] data);

    }
}
