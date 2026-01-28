using Domain.Servisi;
using Domain.Poruke;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Servisi
{
    public class ClientMessageListener
    {
        private readonly INetworkClient _networkClient;
        private readonly IObjectSerializer _serializacija;
        private readonly IMessageHandler _handler;

        private Thread _thread;
        private bool _running;
        private readonly int _idleSleepMs;

        public ClientMessageListener(
            INetworkClient networkClient,
            IObjectSerializer serializer,
            IMessageHandler handler,
            int idleSleepMS = 50
            )
        {
            _networkClient = networkClient;
            _serializacija = serializer;
            _handler = handler;
            _idleSleepMs = idleSleepMS;
        }
        public void Start()
        {
            if (_running) return;

            _running = true;
            _thread =  new Thread(ListenLoop);
            _thread.IsBackground = true;
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;

            try
            {
                 
                if (_thread != null && _thread.IsAlive)
                {
                    _thread.Join(1000);
                }
            }
            catch
            {
                 
            }
        }
        private void ListenLoop()
        {
             while(_running)
            {
              //  Console.WriteLine("Test poll");
                try
                {
                    if(!_networkClient.IsConnected)
                    {
                        Thread.Sleep(200);
                        continue;
                    }

                    if(!_networkClient.TryReceive(out byte[] data))
                    {
                        Thread.Sleep(_idleSleepMs);
                        continue;
                    }
                    if(data == null || data.Length == 0)
                    {
                        Thread.Sleep(_idleSleepMs);
                        continue;
                    }
                    object obj = _serializacija.Deserialize(data);
                    if(obj == null)
                    {
                        continue;
                    }
                    NetworkMessage msg = obj as NetworkMessage;
                    if(msg == null)
                    {
                        continue;
                    }
                    _handler.Handle(msg);

                }catch
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}
