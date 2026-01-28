using Domain.Enumi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Modeli
{
    [Serializable]
    public class Table
    {
        public int Broj;
        public int Kapacitet;
        public TableType Zauzetost;

        public Table() { }
        public Table(int broj,int kapacitet)
        {
            Broj = broj;
            Kapacitet = kapacitet;
            Zauzetost = TableType.SLOBODAN;
        }
    }
}
