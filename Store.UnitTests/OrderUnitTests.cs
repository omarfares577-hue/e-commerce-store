using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Store.API.Controllers;
using Store.API.Errors;
using Store.Application.Contracts;
using Store.Application.Dtos.CreateDtos;
using Store.Application.Dtos.ReturnDtos;
using Store.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
namespace Store.UnitTests
{
    public class OrderUnitTests
    {
        private readonly Mock<IOrderServices> _orderServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly OrderController _controller;

        public OrderUnitTests()
        {
            _orderServiceMock = new Mock<IOrderServices>();
            _mapperMock = new Mock<IMapper>();

            _controller = new OrderController(_orderServiceMock.Object, _mapperMock.Object);
        }

        private void SetUserContext(string email)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Email, email) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }


        [Fact]
        public async Task GetOrderById_WhenOrderExists_ReturnsOkWithOrderDto()
        {
            string email = "test@test.com";
            int orderId = 1;
            var order = new Order { Id = orderId, BuyerEmail = email };
            var orderDto = new OrderDto { Id = orderId };

            SetUserContext(email);

            _orderServiceMock.Setup(s => s.GetOrderByIdAsync(orderId, email)).ReturnsAsync(order);
            _mapperMock.Setup(m => m.Map<OrderDto>(order)).Returns(orderDto);

            var result = await _controller.GetOrderById_ForUser(orderId);
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedDto = okResult.Value.Should().BeOfType<OrderDto>().Subject;
            returnedDto.Id.Should().Be(orderId);
        }

        [Fact]
        public async Task GetOrderById_WhenOrderNotFound_ReturnsNotFound()
        {
            string email = "test@test.com";
            int orderId = 99;
            SetUserContext(email);

            _orderServiceMock.Setup(s => s.GetOrderByIdAsync(orderId, email)).ReturnsAsync((Order?)null);
            var result = await _controller.GetOrderById_ForUser(orderId);
            var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().BeOfType<ApiResponse>()
                .Which.StatusCode.Should().Be(404);
        }


        [Fact]
        public async Task GetDeliveryMethods_ReturnsOkWithMethodsList()
        {
            var deliveryMethods = new List<DeliveryMethod>
        {
            new DeliveryMethod { Id = 1, ShortName = "DHL" }
        };

            _orderServiceMock.Setup(s => s.GetDeliveryMethodsAsync()).ReturnsAsync(deliveryMethods);
            var result = await _controller.GetDeliveryMethods();

            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedList = okResult.Value.Should().BeOfType<List<DeliveryMethod>>().Subject;
            returnedList.Should().HaveCount(1);
        }


        [Fact]
        public async Task CreateOrder_WhenEmailIsEmpty_ReturnsUnauthorized()
        {
            SetUserContext(""); 
            var dto = new CreateOrderDto();

            var result = await _controller.CreateOrder(dto);
            var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.Value.Should().BeOfType<ApiResponse>()
                .Which.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task CreateOrder_WhenOrderServiceReturnsNull_ReturnsBadRequest()
        {
            string email = "test@test.com";
            SetUserContext(email);
            var dto = new CreateOrderDto { DeliveryMethodId = 1, BasketId = "basket1" };
            var mappedAddress = new ShippingAddress();

            _mapperMock.Setup(m => m.Map<AddressDto, ShippingAddress>(dto.ShippingAddress)).Returns(mappedAddress);
            _orderServiceMock.Setup(s => s.CreateOrderAsync(email, dto.DeliveryMethodId, dto.BasketId, mappedAddress,null))
                             .ReturnsAsync((Order?)null);

            var result = await _controller.CreateOrder(dto);

            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeOfType<ApiResponse>()
                .Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task CreateOrder_WithValidData_ReturnsOkWithOrderDto()
        {
            string email = "test@test.com";
            SetUserContext(email);
            var dto = new CreateOrderDto { DeliveryMethodId = 1, BasketId = "basket1" };
            var mappedAddress = new ShippingAddress();
            var createdOrder = new Order { Id = 1, BuyerEmail = email };
            var orderDto = new OrderDto { Id = 1 };

            _mapperMock.Setup(m => m.Map<AddressDto, ShippingAddress>(dto.ShippingAddress)).Returns(mappedAddress);
            _orderServiceMock.Setup(s => s.CreateOrderAsync(email, dto.DeliveryMethodId, dto.BasketId, mappedAddress,null))
                             .ReturnsAsync(createdOrder);
            _mapperMock.Setup(m => m.Map<Order, OrderDto>(createdOrder)).Returns(orderDto);
            var result = await _controller.CreateOrder(dto);

            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedDto = okResult.Value.Should().BeOfType<OrderDto>().Subject;
            returnedDto.Id.Should().Be(1);
        }


        [Fact]
        public async Task GetOrders()
        {
            string email = "test@test.com";
            SetUserContext(email);

            var orders = new List<Order> { new Order { Id = 1, BuyerEmail = email } };
            var orderDtos = new List<OrderDto> { new OrderDto { Id = 1 } };

            _orderServiceMock.Setup(s => s.GetOrdersForUserAsync(email)).ReturnsAsync(orders);
            _mapperMock.Setup(m => m.Map<IReadOnlyList<OrderDto>>(orders)).Returns(orderDtos);
            var result = await _controller.GetOrders();

            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedList = okResult.Value.Should().BeOfType<List<OrderDto>>().Subject;
            returnedList.Should().HaveCount(1);
        }
    }
}

