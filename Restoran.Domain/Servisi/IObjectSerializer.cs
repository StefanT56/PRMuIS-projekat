using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Servisi
{
    public interface IObjectSerializer
    {
        byte[] Serialize(object obj);
        object Deserialize(byte[] data);
    }
}
