using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Modeli
{
    [Serializable]
    public class Reservation
    {
        public int Id;
        public string ImeGosta;
        public int BrojStola;
        public DateTime VrijemePocetak;
        public DateTime VrijemeKraj;
        public Reservation() { } 
        public Reservation(int id,string ime,int brStola,DateTime od,DateTime doo)
        {
            Id = id;
            ImeGosta = ime;
            BrojStola = brStola;
            VrijemePocetak = od;
            VrijemeKraj = doo;
        }
    }
}
