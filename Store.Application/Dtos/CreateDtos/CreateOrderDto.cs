using Store.Application.Dtos.ReturnDtos;
using Store.Domain.Entities;
using Store.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Application.Dtos.CreateDtos
{
    public class CreateOrderDto
    {
        [Required(ErrorMessage = "Basket ID is required")]
        public string BasketId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Delivery method is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid Delivery Method ID")]
        public int DeliveryMethodId { get; set; }

        [Required(ErrorMessage = "Shipping address is required")]
        public AddressDto ShippingAddress { get; set; } = null!;

    }
}
