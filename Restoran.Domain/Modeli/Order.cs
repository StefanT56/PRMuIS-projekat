using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Modeli
{
    [Serializable]
    public class Order
    {
        public int id;
        public int BrojStola;
        public List<OrderItem> Stavke;
        public DateTime VremeKreiranja;
     
        public Order()
        {
            Stavke = new List<OrderItem>();
            VremeKreiranja = DateTime.Now;
        }
    }
}
