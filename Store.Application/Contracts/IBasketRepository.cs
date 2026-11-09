using Store.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Application.Contracts
{
    public interface IBasketRepository
    {
        Task<CustomerBasket?> GetBasketAsync(string basketid);
        Task<CustomerBasket?> UpdateBasketAsync(CustomerBasket customer);
        Task<bool> DeleteBasketAsync(string basketid);
    }
}
