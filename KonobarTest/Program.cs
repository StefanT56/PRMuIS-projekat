using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace KonobarTest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string host = "127.0.0.1";
            int port = 5000;

            Console.OutputEncoding = Encoding.UTF8;

            try
            {
                using (var client = new TcpClient())
                {
                    Console.WriteLine($"Povezujem se na {host}:{port} ...");
                    await client.ConnectAsync(host, port);

                    using (NetworkStream ns = client.GetStream())
                    {
                        // 1) HELLO (obavezno)
                        await WriteFrameAsync(ns, "HELLO|role=WAITER|name=TestKonobar");
                        Console.WriteLine("S> " + await ReadFrameAsync(ns));

                        Console.WriteLine("\nKomande (primeri):");
                        Console.WriteLine("  PING");
                        Console.WriteLine("  TABLES_GET");
                        Console.WriteLine("  ORDER_ADD|table=5|items=Kafa:2,Pica:1...");
                        Console.WriteLine("  BILL_GET|table=5");
                        Console.WriteLine("  BILL_PAY|table=5|paid=2000");
                        Console.WriteLine("  READY_GET|table=5");
                        Console.WriteLine("  exit\n");

                        // 2) Ručno šalješ poruke
                        while (true)
                        {
                            Console.Write("C> ");
                            string line = Console.ReadLine();

                            if (string.IsNullOrWhiteSpace(line))
                                continue;

                            if (line.Equals("exit", StringComparison.OrdinalIgnoreCase))
                                break;
                            await WriteFrameAsync(ns, line);
                            string resp = await ReadFrameAsync(ns);

                            if (resp == null)
                            {
                                Console.WriteLine("S> (server je zatvorio konekciju)");
                                break;
                            }

                            Console.WriteLine("S> " + resp);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greška: " + ex.Message);
            }

            Console.WriteLine("Kraj. ENTER...");
            Console.ReadLine();
        }

        // ---- framing: 4 bajta dužina (big-endian) + UTF8 payload ----

        private static async Task WriteFrameAsync(NetworkStream ns, string message)
        {
            byte[] payload = Encoding.UTF8.GetBytes(message ?? "");
            int len = payload.Length;

            byte[] lenBuf = new byte[]
            {
                (byte)((len >> 24) & 0xFF),
                (byte)((len >> 16) & 0xFF),
                (byte)((len >> 8) & 0xFF),
                (byte)(len & 0xFF)
            };

            await ns.WriteAsync(lenBuf, 0, 4);
            await ns.WriteAsync(payload, 0, payload.Length);
            await ns.FlushAsync();
        }

        private static async Task<string> ReadFrameAsync(NetworkStream ns)
        {
            byte[] lenBuf = await ReadExactAsync(ns, 4);
            if (lenBuf == null) return null;

            int len = (lenBuf[0] << 24) | (lenBuf[1] << 16) | (lenBuf[2] << 8) | lenBuf[3];
            if (len < 0 || len > 1024 * 1024) throw new InvalidDataException("Nevalidna dužina poruke.");

            byte[] payload = await ReadExactAsync(ns, len);
            if (payload == null) return null;

            return Encoding.UTF8.GetString(payload);
        }

        private static async Task<byte[]> ReadExactAsync(NetworkStream ns, int count)
        {
            byte[] buf = new byte[count];
            int off = 0;

            while (off < count)
            {
                int n = await ns.ReadAsync(buf, off, count - off);
                if (n == 0) return null; // konekcija zatvorena
                off += n;
            }

            return buf;
        }
    }
}
