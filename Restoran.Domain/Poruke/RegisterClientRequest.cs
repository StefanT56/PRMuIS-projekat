using Domain.Enumi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Poruke
{
    [Serializable]
    public class RegisterClientRequest
    {
        public ClientRole Role;
    }
}
