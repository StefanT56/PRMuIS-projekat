using System.Net.Sockets;

namespace Core.Models
{
    public class InfoKlijenta
    {
        public int Id { get; set; }
        public TipOsoblja Tip { get; set; }
        public Socket Soket { get; set; }
    }
}
