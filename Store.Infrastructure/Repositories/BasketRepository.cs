using Store.Application.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;
using Store.Domain.Entities;
namespace Store.Infrastructure.Repositories
{
    public class BasketRepository : IBasketRepository
    {
        private readonly IDatabase _database;
        public BasketRepository(IConnectionMultiplexer redis)
        { _database = redis.GetDatabase(); }
        public async Task<bool> DeleteBasketAsync(string basketid)
        {
            return await _database.KeyDeleteAsync(basketid);
        }
        public async Task<CustomerBasket?> GetBasketAsync(string basketid)
        {
            var basket = await _database.StringGetAsync(basketid);

            // الصح هنا إننا نشيل الـ ! عشان لو السلة فاضية ترجع null، ولو موجودة تكمل وتعمل Deserialize
            if (basket.IsNullOrEmpty)
            {
                return null;
            }

            return JsonSerializer.Deserialize<CustomerBasket?>(basket!);
        }
        public async Task<CustomerBasket?> UpdateBasketAsync(CustomerBasket customer)
        {
            var exist = await _database.StringSetAsync(customer.Id, JsonSerializer.Serialize(customer), TimeSpan.FromDays(30));
            if (!exist)
                return null;
            return await GetBasketAsync(customer.Id);
        }
    }
}
