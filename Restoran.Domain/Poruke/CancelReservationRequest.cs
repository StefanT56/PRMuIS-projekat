using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Poruke
{
    [Serializable]
    public class CancelReservationRequest
    {
        public int ReservationId;
    }
}

//ako se pitas sto nema konstruktora , razlog je sto formater radi direktno sa memorijom , u sustini ovo su kao strukture u c 