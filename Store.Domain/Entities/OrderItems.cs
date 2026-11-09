using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Domain.Entities
{
    public class OrderItems
    {
        public OrderItems() { }
        public OrderItems(ProductItemOrdered itemordered, decimal price, int quantity)
        {
            ItemOrdered = itemordered;
            Price = price;
            Quantity = quantity;
        }
        public int Id { get; set; }
        public ProductItemOrdered ItemOrdered { get; set; } = null!; 
        public decimal Price { get; set; }  
        public int Quantity { get; set; }
    }
}
