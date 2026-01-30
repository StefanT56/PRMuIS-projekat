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
        public int id;
        public string Naziv;
        public OrderItemType Kategorija;
        // true-pice false-hrana 
        public double cena;
        public OrderStatus Status;

        public int KonobarId { get; set; }
        public int BrojStola { get; set; }


        public OrderItem() { }  
    }
}
