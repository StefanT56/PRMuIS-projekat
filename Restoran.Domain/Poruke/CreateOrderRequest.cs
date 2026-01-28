using Domain.Modeli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Poruke
{
    [Serializable]
    public class CreateOrderRequest
    {
        public int BrojStola;
        public List<OrderItem> stavke;
        public CreateOrderRequest()
        {
            stavke = new List<OrderItem>();
        }
    }
}
