using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Application.Dtos.ReturnDtos
{
    public class ProductDto
    {
        public int Id { get; set; }
        public  string Name { get; set; } =string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? PictureUrl { get; set; }
        public string CategoryName { get; set; } = string.Empty;

    }
}
