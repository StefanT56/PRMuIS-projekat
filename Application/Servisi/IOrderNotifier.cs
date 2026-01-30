using Domain.Modeli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Servisi
{
    public interface IOrderNotifier
    {
        Task NotifyNewItemAsync(int itemId, int orderId, OrderItem item);
    }
}
