using Microsoft.Extensions.Configuration;
using Store.Application.Contracts;
using Store.Application.Specification;
using Store.Domain.Entities;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Store.Infrastructure.Repositories
{
    public class PaymentService : IPaymentService
    {
        private readonly IBasketRepository _basketRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;

        public PaymentService(IBasketRepository basketRepo, IUnitOfWork unitOfWork, IConfiguration config)
        {
            _basketRepository = basketRepo;
            _unitOfWork = unitOfWork;
            _config = config;
        }
       public async Task<CustomerBasket?> CreateOrUpdatePaymentIntent(string basketId)
        {
            StripeConfiguration.ApiKey = _config["StripeSettings:Secretkey"];
            var basket = await _basketRepository.GetBasketAsync(basketId);
            if (basket is null)
                return null;
            basket.ShippingPrice = 0m;
            if (basket.DeliveryMethodId.HasValue)
            {
                var delivery = await _unitOfWork.DeliveryMethods.GetByIdAsync(basket.DeliveryMethodId.Value);
                if (delivery is not null)
                    basket.ShippingPrice = delivery.Cost;
            }  
            foreach(var item in basket.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.Id);
                if (product is not null)
                    if (product.Price != item.Price)
                        item.Price = product.Price;
            }
            var service = new PaymentIntentService();
            PaymentIntent paymentintent;
            if(string.IsNullOrEmpty(basket.PaymentIntentId))
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(basket.Items.Sum(item => item.Price * 100 * item.Quantity ) + basket.ShippingPrice * 100),
                    Currency = "usd",
                    AllowedPaymentMethodTypes = new List<string> { "card" }
                };
                paymentintent = await service.CreateAsync(options);
                basket.PaymentIntentId = paymentintent.Id;
                basket.ClientSecret = paymentintent.ClientSecret;

            }
            else
            {
                var options = new PaymentIntentUpdateOptions
                {
                    Amount = (long)(basket.Items.Sum(i => i.Quantity * i.Price * 100) + (basket.ShippingPrice * 100))
                };
                await service.UpdateAsync(basket.PaymentIntentId, options);
            }

            await _basketRepository.UpdateBasketAsync(basket);

            return basket;
        }
        public async Task<Order> UpdateOrderPaymentSucceeded(string paymentIntentId)
        {
            var spec = new OrderWithPaymentIntentSpecification(paymentIntentId);
            var order = await _unitOfWork.Orders.GetByIdWithSpecAsync(spec);
            if (order is null)
                return null!;
            order.Status = OrderStatus.PaymentReceived;
            _unitOfWork.Orders.Update(order);
            await _unitOfWork.CompleteAsync();
            return order;
        }
        public async Task<Order> UpdateOrderPaymentFailed(string paymentIntentId)
        {
            var spec = new OrderWithPaymentIntentSpecification(paymentIntentId);
            var order = await _unitOfWork.Orders.GetByIdWithSpecAsync(spec);

            if (order == null) return null!;

            order.Status = OrderStatus.PaymentFailed;
            _unitOfWork.Orders.Update(order);
            await _unitOfWork.CompleteAsync();

            return order;
        }
    }

}

