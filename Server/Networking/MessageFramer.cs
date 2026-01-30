using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Networking
{
    public static class MessageFramer
    {
        public static async Task<string> ReadFrameAsync(NetworkStream ns, CancellationToken ct)
        {
            byte[] lenBuf = await ReadExactAsync(ns, 4, ct).ConfigureAwait(false);
            if (lenBuf == null)
                return null;
            
            int len = (lenBuf[0]<<24) | (lenBuf[1]<<16) | (lenBuf[2]<<8) | lenBuf[3];
            if (len < 0 || len > 1024 * 1024)
                throw new InvalidDataException("Losa duzina poruke");

            byte[] payload = await ReadExactAsync(ns, len, ct).ConfigureAwait(false);
            if (payload == null)
                return null;

            return Encoding.UTF8.GetString(payload);
        }

        public static async Task WriteFrameAsync(NetworkStream ns, string message, CancellationToken ct)
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

            await ns.WriteAsync(lenBuf, 0, 4, ct).ConfigureAwait(false);
            await ns.WriteAsync(payload, 0, payload.Length, ct).ConfigureAwait(false);
            await ns.FlushAsync(ct).ConfigureAwait(false);
        }

        private static async Task<byte[]> ReadExactAsync(NetworkStream ns, int count, CancellationToken ct)
        {
            byte[] buf = new byte[count];
            int off = 0;

            while (off < count)
            {
                int n = await ns.ReadAsync(buf, off, count - off, ct).ConfigureAwait(false);
                if (n == 0)
                    return null;
                off += n;
            }
            return buf;
        }
    }
}
