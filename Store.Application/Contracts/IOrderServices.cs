using Store.Domain.Entities;
using Store.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Application.Contracts
{
  public interface IOrderServices
  {
            Task<Order?> CreateOrderAsync(string buyerEmail, int deliveryMethodId, string basketId, ShippingAddress shippingAddress, string? paymentIntentId = null);
            Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string buyerEmail);
            Task<Order?> GetOrderByIdAsync(int orderId, string buyerEmail);
            Task<IReadOnlyList<DeliveryMethod>> GetDeliveryMethodsAsync();
  }
}

