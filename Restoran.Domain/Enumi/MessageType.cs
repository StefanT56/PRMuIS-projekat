using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enumi
{
   
    public enum MessageType
    {
        RegisterClient,   //klijent serveru 
        CreateOrder,  // konobar serveru
        OrderAccepted, // server konobaru
        TaskAssigned, // server kuvaru ili barmenu 
        TaskDone, // kuvar/barmen serveru
        OrderReady, //server konobaru
        CreateReservation, // menadzer serveru 
        CancelReservation, // menadzer serveru
        Error // server klijentu bilo kojem u slucaju greske 
 
    }
}
