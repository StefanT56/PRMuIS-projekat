using Application.Servisi;
using Domain.Enumi;
using Domain.Modeli;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server.Networking
{
    public sealed class UdpNotifier : IOrderNotifier
    {
        private readonly UdpClient _udp = new UdpClient();
        private readonly UdpRoleRegistry _registry;

        public UdpNotifier(UdpRoleRegistry registry)
        {
            _registry = registry;
        }

        public async Task NotifyNewItemAsync(int itemId, int orderId, OrderItem item)
        {
            string msg =
                $"ITEM_NEW|itemId={itemId}|orderId={orderId}|table={item.BrojStola}|name={item.Naziv}|type={item.Kategorija}|status={item.Status}";

            byte[] data = Encoding.UTF8.GetBytes(msg);

            if (item.Kategorija == OrderItemType.Hrana &&
                _registry.TryGetEndPoint(ClientRole.Kuvar, out var cookEp))
                await _udp.SendAsync(data, data.Length, cookEp);

            if (item.Kategorija == OrderItemType.Pice &&
                _registry.TryGetEndPoint(ClientRole.Barmen, out var barEp))
                await _udp.SendAsync(data, data.Length, barEp);
        }
    }
}
