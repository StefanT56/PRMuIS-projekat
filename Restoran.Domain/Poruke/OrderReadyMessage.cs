using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Poruke
{
    [Serializable]
    public class OrderReadyMessage
    {
        public int OrderId; // koja je spremna 
        public int BrojStola; // za koji sto
    }
}
