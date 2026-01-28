using Domain.Enumi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Modeli
{
    [Serializable]
    public class OrderItem
    {
        public string Naziv;
        public int Kolicina;
        public OrderItemType Tip;
        // true-pice false-hrana 
        public OrderItem() { }  
    }
}
