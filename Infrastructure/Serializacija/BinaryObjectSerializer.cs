using Domain.Servisi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Serializacija
{
    public class BinaryObjectSerializer : IObjectSerializer
    {
        public object Deserialize(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using(MemoryStream ms = new MemoryStream())
                {
                    return formatter.Deserialize(ms);
                }

            }catch
            {
                return null;
            }
        }

        public byte[] Serialize(object obj)
        {
            if (obj == null) return Array.Empty<byte>();
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using(MemoryStream ms = new MemoryStream())
                {
                    formatter.Serialize(ms, obj);
                    return ms.ToArray();
                }
            }catch
            {
                return Array.Empty<byte>();
            }
        }
    }
}
