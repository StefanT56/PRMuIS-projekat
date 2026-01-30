using Domain.Enumi;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server.Networking
{
    public sealed class UdpRoleRegistry
    {
        private readonly ConcurrentDictionary<ClientRole, IPEndPoint> _endPoints = new ConcurrentDictionary<ClientRole, IPEndPoint> (); 

        public void setEndPoint(ClientRole role, IPEndPoint endPoint)
        {
            _endPoints[role] = endPoint;
        }  
       
        public bool TryGetEndPoint(ClientRole role, out IPEndPoint ep) => _endPoints.TryGetValue (role, out ep);
    }
}
