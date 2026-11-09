using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Store.Application.Dtos.UpdateDtos
{
    public class UpdateBasketItemDto
    {
        [Required(ErrorMessage = "Basket ID is required")]
        public string BasketId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Items list is required")]
        public List<BasketItemDto> Items { get; set; } = new List<BasketItemDto>();
    }

    public class BasketItemDto
    {
        [Required(ErrorMessage = "Product ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid Product ID")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }
}    

