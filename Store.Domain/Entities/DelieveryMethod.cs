using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Domain.Entities
{
    public class DeliveryMethod
    {
        public int Id { get; set; }
        public string ShortName { get; set; } = string.Empty;
        public string DeliveryTime { get; set; } = string.Empty; 
        public string Description { get; set; } = string.Empty;
        public decimal Cost { get; set; }
    }
}
