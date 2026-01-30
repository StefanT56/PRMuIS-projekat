using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TestKuvarUdp
{
    internal class Program
    {
        static async Task Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            using (var udp = new UdpClient(0))
            { // random local port
                var serverEp = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5001);
                var serverStatusEp = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5002);

                // registracija
                byte[] reg = Encoding.UTF8.GetBytes("REGISTER|role=COOK");
                await udp.SendAsync(reg, reg.Length, serverEp);

                var ack = await udp.ReceiveAsync();
                Console.WriteLine("S> " + Encoding.UTF8.GetString(ack.Buffer));

                Console.WriteLine("Kuvar sluša porudžbine (UDP)...");

                while (true)
                {
                    UdpReceiveResult res = await udp.ReceiveAsync();
                    string msg = Encoding.UTF8.GetString(res.Buffer);

                    Console.WriteLine("ORDER> " + msg);

                    // očekujemo: ITEM_NEW|itemId=12|...
                    if (TryExtractItemId(msg, out int itemId))
                    {
                        Console.WriteLine($"[TEST] Startujem stavku {itemId}");

                        byte[] start = Encoding.UTF8.GetBytes($"ITEM_START|itemId={itemId}");
                        await udp.SendAsync(start, start.Length, serverStatusEp);

                        await Task.Delay(1000); // simulacija pripreme

                        Console.WriteLine($"[TEST] Završavam stavku {itemId}");
                        byte[] done = Encoding.UTF8.GetBytes($"ITEM_DONE|itemId={itemId}");
                        await udp.SendAsync(done, done.Length, serverStatusEp);
                    }
                }
            }
        }
        static bool TryExtractItemId(string msg, out int itemId)
        {
            itemId = 0;
            if (string.IsNullOrWhiteSpace(msg)) return false;

            int i = msg.IndexOf("itemId=", StringComparison.OrdinalIgnoreCase);
            if (i < 0) return false;

            i += "itemId=".Length;
            int end = msg.IndexOf('|', i);
            if (end < 0) end = msg.Length;

            return int.TryParse(msg.Substring(i, end - i), out itemId);
        }
    }
}
