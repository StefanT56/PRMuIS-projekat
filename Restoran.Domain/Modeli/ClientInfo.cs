using Domain.Enumi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Modeli
{
    [Serializable]
    public class ClientInfo
    {
        public int Id;
        public ClientRole role;
        public string ImeZaposlenog; //opciono 

        public ClientInfo() { }
        public ClientInfo(int id , ClientRole r,string ime)
        {
            Id = id;
            role = r;
            ImeZaposlenog = ime;
        }
    }
}
