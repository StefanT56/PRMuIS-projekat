using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Poruke
{
    [Serializable]
    public class CreateReservationRequest
    {
        public string ImeGosta;
        public int BrojStola;
        public DateTime VremePocetkaRezervacije;
        public DateTime VremeKrajaRezervacije;
    }
}
