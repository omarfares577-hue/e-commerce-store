using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using Store.API.Errors;
using Store.API.Helpers;
using Store.Application;
using Store.Application.Contracts;
using Store.Application.Dtos.CreateDtos;
using Store.Application.Dtos.Identity;
using Store.Application.Dtos.ReturnDtos;
using Store.Domain.Entities;
using Store.Domain.Entities.Identity;
using Store.Infrastructure.Identity;
using Stripe.Climate;
using System.Net;
using System.Security.Claims;
using static Store.Application.Contracts.IOrderServices;

namespace Store.API.Controllers
{
    public static class ClaimsPrincipalExtensions
    {
        public static string RetrieveEmailFromPrincipal(this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.Email)!;
        }
    }
    public class OrderController : BaseApiController
    {
        private readonly IOrderServices _orderService;
        private readonly IMapper _mapper;
        public OrderController(  IOrderServices orderService, IMapper mapper)
        {
            _orderService = orderService;
            _mapper = mapper;
        }
        [Authorize(Roles = "Client")]
        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto dto)
        {
            var email = HttpContext.User.RetrieveEmailFromPrincipal();
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new ApiResponse(401, "UnAuthorized Please Log in"));

            var address = _mapper.Map<AddressDto, ShippingAddress>(dto.ShippingAddress);

            var order = await _orderService.CreateOrderAsync(email, dto.DeliveryMethodId, dto.BasketId, address);

            if (order == null)
                return BadRequest(new ApiResponse(400, "Order Faild,Verify the contents of your cart"));


            return Ok(_mapper.Map<Domain.Entities.Order, OrderDto>(order));
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrders()
        {
            var email = HttpContext.User.RetrieveEmailFromPrincipal();

            var orders = await _orderService.GetOrdersForUserAsync(email);

            return Ok(_mapper.Map<IReadOnlyList<OrderDto>>(orders));
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrderById_ForUser(int id)
        {
            var email = HttpContext.User.RetrieveEmailFromPrincipal();

            var order = await _orderService.GetOrderByIdAsync(id, email);

            if (order == null) return NotFound(new ApiResponse(404));

            return Ok(_mapper.Map<OrderDto>(order));
        }
        [AllowAnonymous]
        [HttpGet("delivery-methods")]
        public async Task<ActionResult<IReadOnlyList<DeliveryMethod>>> GetDeliveryMethods()
        {
            return Ok(await _orderService.GetDeliveryMethodsAsync());
        }
    }
}
