using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Poruke
{
    [Serializable]
    public class TaskDoneRequest
    {
        public int TaskId; // id zadatka koji je zavrsen 
    }
}
