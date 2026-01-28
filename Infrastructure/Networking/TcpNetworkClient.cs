using Domain.Servisi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Networking
{
    public class TcpNetworkClient : INetworkClient
    {

        private Socket _socket;
        private readonly byte[] _dolaziBuffer = new byte[8192];
        public bool IsConnected {
            get
            {
                try
                {
                    return _socket != null && _socket.Connected;
                }catch
                {
                    return false;
                }
            }
        
        }

        public void Connect(string serverAddress, int port)
        {
            try
            {
                Disconnect();
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverAddress), port);
                _socket.Connect(serverEndPoint);
            }catch
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
             try
            {
                if(_socket != null)
                {
                    try
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                    }catch
                    {

                    }
                    _socket.Close();
                }
            }catch
            {

            }
            finally
            {
                _socket = null;
            }
        }

        public byte[] Receive()
        {
            if (!IsConnected) return Array.Empty<byte>();
            // ako nije kontekotvana moguce je da je smece pa saljemo niz praznih bajtova 
            try
            {
                int bytesReceived = _socket.Receive(_dolaziBuffer);
                if(bytesReceived <= 0)
                {
                    Disconnect();
                    return Array.Empty<byte>();
                }

                byte[] data = new byte[bytesReceived];
                Buffer.BlockCopy(_dolaziBuffer, 0, data, 0, bytesReceived);
                return data;
            }catch
            {
                Disconnect();
                return Array.Empty<byte>();
            }
        }

        public void Send(byte[] data)
        {
            if (data == null || data.Length == 0) return;
            if (!IsConnected) return;

            try
            {
                _socket.Send(data);
            }catch
            {
                Disconnect();
            }
        }
        public bool TryReceive(out byte[] data)
        {
            data = new byte[0];
            if (_socket == null || !_socket.Connected) return false;

            if (!_socket.Poll(1000 * 1000, SelectMode.SelectRead)) return false;
            byte[] buffer = new byte[8192];
            int bytesRead = _socket.Receive(buffer);
            if (bytesRead <= 0) return false;

            data = new byte[bytesRead];
            Buffer.BlockCopy(buffer, 0, data, 0, bytesRead);
            return true;


        }
    }
}
