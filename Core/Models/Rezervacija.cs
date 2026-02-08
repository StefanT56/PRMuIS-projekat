using System;

namespace Core.Models
{
    [Serializable]
    public class Rezervacija
    {
        public int BrojRezervacije { get; set; }
        public int BrojStola { get; set; }
        public int BrojGostiju { get; set; }
        public DateTime VremeOd { get; set; }
        public DateTime VremeDo { get; set; }
        public bool JeAktivna { get; set; }


        public DateTime VremeRezervacije => VremeOd;
        public DateTime VremeIsteka => VremeDo;

        public Rezervacija()
        {
            JeAktivna = true;
        }

        public override string ToString()
        {
            return string.Format("Rez#{0} | Sto {1} | {2} gostiju | {3:HH:mm}-{4:HH:mm} | {5}",
                BrojRezervacije,
                BrojStola,
                BrojGostiju,
                VremeOd,
                VremeDo,
                JeAktivna ? "AKTIVNA" : "ISTEKLA");
        }
    }
}