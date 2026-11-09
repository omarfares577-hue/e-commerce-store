using Microsoft.AspNetCore.Identity;
using Store.Application.Contracts;
using Store.Application.Specification;
using Store.Domain.Entities;
using Store.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Infrastructure.Repositories
{
    public class OrderServices : IOrderServices
    {
        private readonly IPaymentService _paymentService;
        private readonly IBasketRepository _basketrepository;
        private readonly IUnitOfWork _unitofwork;
        private readonly UserManager<AppUser> _userManager;
        public OrderServices(IPaymentService paymentService, UserManager<AppUser> manager, IBasketRepository basketrepository, IUnitOfWork unitofwork)
        {
            _paymentService = paymentService;
            _basketrepository = basketrepository;
            _unitofwork = unitofwork;
            _userManager = manager;
        }
        public async Task<Order?> CreateOrderAsync(string buyerEmail, int deliveryMethodId, string basketId, ShippingAddress shippingAddress, string? paymentIntentId = null)
        {
            var basket = await _basketrepository.GetBasketAsync(basketId);
            if (basket == null) return null;

            var user = await _userManager.FindByEmailAsync(buyerEmail);
            if (user == null) return null;

            var finalPaymentId = paymentIntentId ?? basket.PaymentIntentId;
            finalPaymentId ??= Guid.NewGuid().ToString();
            var items = new List<OrderItems>();
            foreach (var item in basket.Items)
            {
                var productItem = await _unitofwork.Products.GetByIdAsync(item.Id);
                if (productItem == null) continue;

                var itemOrdered = new ProductItemOrdered(productItem.Id, productItem.Name, productItem.PictureUrl!);
                items.Add(new OrderItems(itemOrdered, productItem.Price, item.Quantity));
            }
            if (!items.Any()) return null;

            var delivery = await _unitofwork.DeliveryMethods.GetByIdAsync(deliveryMethodId);
            if (delivery == null) return null;

            var subtotal = items.Sum(i => i.Price * i.Quantity);

            // حذف الأوردر القديم لو موجود بنفس الـ PaymentIntentId
            var spec = new OrderWithPaymentIntentSpecification(finalPaymentId);
            var existingOrder = await _unitofwork.Orders.GetByIdWithSpecAsync(spec);
            if (existingOrder != null)
            {
                _unitofwork.Orders.Delete(existingOrder);
                await _unitofwork.CompleteAsync();
            }

            await _paymentService.CreateOrUpdatePaymentIntent(basket.Id);

            // Address جديد تماماً عشان نتفادى أي Id أو tracking قديم
            var address = new ShippingAddress
            {
                 FirstName = shippingAddress.FirstName,
                 LastName  = shippingAddress.LastName,
                 Street    = shippingAddress.Street,
                 City      = shippingAddress.City,
                 State   = shippingAddress.State,
                 ZipCode = shippingAddress.ZipCode
            };

            var order = new Order(items, buyerEmail, address, delivery, subtotal, finalPaymentId);
            await _unitofwork.Orders.AddAsync(order);

            var result = await _unitofwork.CompleteAsync();
            if (result <= 0) return null;

            await _basketrepository.DeleteBasketAsync(basketId);
            return order;
        }
    
    
        public async Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string buyerEmail)
        {
            var spec = new OrderWithItemsSpecification(buyerEmail);
            return await _unitofwork.Orders.GetAllWithSpecAsync(spec);

        }

        public async Task<Order?> GetOrderByIdAsync(int orderId, string buyerEmail)
        {
            var spec = new OrderWithItemsSpecification(buyerEmail);
            return await _unitofwork.Orders.GetByIdWithSpecAsync(spec);
        }

        public async Task<IReadOnlyList<DeliveryMethod>> GetDeliveryMethodsAsync()
        {
            return await _unitofwork.DeliveryMethods.GetAllAsync();
        }

    }
}
