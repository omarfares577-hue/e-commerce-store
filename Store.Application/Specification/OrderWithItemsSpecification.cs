using Store.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Application.Specification
{
    public class OrderWithItemsSpecification:BaseSpecification<Order>
    {
        public OrderWithItemsSpecification(int id, string buyerEmail)
        : base(o => o.Id == id && o.BuyerEmail == buyerEmail)
        {
            AddInclude(o => o.OrderItems);
            AddInclude(o => o.DeliveryMethod);
        }

        public OrderWithItemsSpecification(string buyerEmail)
            : base(o => o.BuyerEmail == buyerEmail)
        {
            AddInclude(o => o.OrderItems);
            AddInclude(o => o.DeliveryMethod);
            AddOrderByDescending(o => o.OrderDate.UtcDateTime);
        }
    }
}
