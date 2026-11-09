using Store.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Store.Domain.Entities
{
    public enum OrderStatus
    {
        [EnumMember(Value = "Pending")]
        Pending,

        [EnumMember(Value = "Payment Received")]
        PaymentReceived, 

        [EnumMember(Value = "Payment Failed")]
        PaymentFailed 
    }
    public class Order
    {
        public Order()
        {
        }

        public Order(ICollection<OrderItems> orderItems, string buyerEmail, ShippingAddress shippingAddress,
                     DeliveryMethod deliveryMethod, decimal subtotal, string paymentIntentId)
        {
            BuyerEmail = buyerEmail;
            ShippingAddress = shippingAddress;
            DeliveryMethod = deliveryMethod;
            OrderItems = orderItems;
            Subtotal = subtotal;
            PaymentIntentId = paymentIntentId;
        }

        public int Id { get; set; }
        public string BuyerEmail { get; set; } = string.Empty;
        public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;
        public DeliveryMethod DeliveryMethod { get; set; } = null!;
        public ICollection<OrderItems> OrderItems { get; set; } = new List<OrderItems>();
        public ShippingAddress ShippingAddress { get; set; } = null!;
        public decimal Subtotal { get; set; } 
        public OrderStatus Status { get; set; } = OrderStatus.Pending; 
        public string PaymentIntentId { get; set; } = string.Empty; 

        public decimal GetTotal() => Subtotal + DeliveryMethod.Cost;
    }
}

