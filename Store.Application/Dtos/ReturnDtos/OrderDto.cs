using Store.Domain.Entities;
using Store.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Application.Dtos.ReturnDtos
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string BuyerEmail { get; set; } = string.Empty;
        public DateTimeOffset OrderDate { get; set; }
        public AddressDto ShippingAddress { get; set; } = null!;
        public string DeliveryMethod { get; set; } = string.Empty;
        public IReadOnlyList<OrderItemsDto> OrderItems { get; set; } = new List<OrderItemsDto>();
        public decimal Subtotal { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentIntentId { get; set; } = string.Empty;

        // التعديل هنا: تحويلها لـ Property وحسابها أوتوماتيك
        public decimal Total { get; set; }
    }
}