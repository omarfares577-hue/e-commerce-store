using Microsoft.AspNetCore.Mvc;
using Moq;
using Store.API.Controllers;
using Store.API.Errors;
using Store.Application.Contracts;
using Store.Application.Dtos.UpdateDtos;
using Store.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
namespace Store.UnitTests
{
    public class BasketUnitTest
    {
        private readonly Mock<IBasketRepository> _basketRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGenericRepository<Product>> _productRepoMock;
        private readonly BasketController _controller;

        public BasketUnitTest()
        {
            _basketRepoMock = new Mock<IBasketRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _productRepoMock = new Mock<IGenericRepository<Product>>();

            _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

            _controller = new BasketController(_unitOfWorkMock.Object,_basketRepoMock.Object);
        }


        [Fact]
        public async Task GetBasketById_WhenBasketExists_ReturnsOkWithBasket()
        {
            string basketId = "basket_1";
            var basket = new CustomerBasket(basketId);
            _basketRepoMock.Setup(r => r.GetBasketAsync(basketId)).ReturnsAsync(basket);
            var result = await _controller.GetBasketById(basketId);

            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedBasket = okResult.Value.Should().BeOfType<CustomerBasket>().Subject;
            returnedBasket.Id.Should().Be(basketId);
        }

        [Fact]
        public async Task GetBasketById_WhenBasketNotFound_ReturnsNotFound()
        {
            string basketId = "basket_99";
            _basketRepoMock.Setup(r => r.GetBasketAsync(basketId)).ReturnsAsync((CustomerBasket?)null);

            var result = await _controller.GetBasketById(basketId);

            var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().BeOfType<ApiResponse>()
                .Which.StatusCode.Should().Be(404);
        }


        [Fact]
        public async Task CreateBasket_WhenBasketAlreadyExists_ReturnsBadRequest()
        {
            string basketId = "basket_1";
            var existingBasket = new CustomerBasket(basketId);
            _basketRepoMock.Setup(r => r.GetBasketAsync(basketId)).ReturnsAsync(existingBasket);
            var result = await _controller.CreateBasket(basketId);

            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateBasket_WhenBasketDoesNotExist_ReturnsCreated()
        {
            string basketId = "basket_new";
            _basketRepoMock.Setup(r => r.GetBasketAsync(basketId)).ReturnsAsync((CustomerBasket?)null);
            _basketRepoMock.Setup(r => r.UpdateBasketAsync(It.IsAny<CustomerBasket>()))
                          .ReturnsAsync((CustomerBasket b) => b);

            var result = await _controller.CreateBasket(basketId);

            var createdResult = result.Result.Should().BeOfType<CreatedResult>().Subject;
            var createdBasket = createdResult.Value.Should().BeOfType<CustomerBasket>().Subject;
            createdBasket.Id.Should().Be(basketId);
        }


        [Fact]
        public async Task UpdateBasket_WhenProductNotFound_ReturnsNotFound()
        {
            var updateDto = new UpdateBasketItemDto
            {
                BasketId = "basket_1",
                Items = new List<BasketItemDto> { new BasketItemDto { Id = 99, Quantity = 2 } }
            };

            _basketRepoMock.Setup(r => r.GetBasketAsync(updateDto.BasketId)).ReturnsAsync(new CustomerBasket(updateDto.BasketId));
            _productRepoMock.Setup(p => p.GetByIdAsync(99)).ReturnsAsync((Product?)null);

            var result = await _controller.UpdateBasket(updateDto);

            var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateBasket_WithValidItems_ReturnsOkWithUpdatedBasket()
        {
            var updateDto = new UpdateBasketItemDto
            {
                BasketId = "basket_1",
                Items = new List<BasketItemDto> { new BasketItemDto { Id = 1, Quantity = 2 } }
            };

            var basket = new CustomerBasket(updateDto.BasketId);
            var product = new Product { Id = 1, Name = "Test Product", Price = 100 };

            _basketRepoMock.Setup(r => r.GetBasketAsync(updateDto.BasketId)).ReturnsAsync(basket);
            _productRepoMock.Setup(p => p.GetByIdAsync(1)).ReturnsAsync(product);
            _basketRepoMock.Setup(r => r.UpdateBasketAsync(It.IsAny<CustomerBasket>()))
                          .ReturnsAsync((CustomerBasket b) => b);

            var result = await _controller.UpdateBasket(updateDto);

            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var updatedBasket = okResult.Value.Should().BeOfType<CustomerBasket>().Subject;
            updatedBasket.Items.Should().HaveCount(1);
            updatedBasket.Items[0].Quantity.Should().Be(2);
        }


        [Fact]
        public async Task RemoveItem_WhenBasketNotFound_ReturnsNotFound()
        {
            string basketId = "basket_99";
            int productId = 1;
            _basketRepoMock.Setup(r => r.GetBasketAsync(basketId)).ReturnsAsync((CustomerBasket?)null);
            var result = await _controller.RemoveItemFromBasket(basketId, productId);

            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().BeOfType<ApiResponse>()
                .Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task RemoveItem_WhenBasketExists_ReturnsNoContent()
        {
            string basketId = "basket_1";
            int productId = 1;
            var basket = new CustomerBasket(basketId);
            basket.Items.Add(new BasketItem { Id = productId, Quantity = 1 });

            _basketRepoMock.Setup(r => r.GetBasketAsync(basketId)).ReturnsAsync(basket);
            _basketRepoMock.Setup(r => r.UpdateBasketAsync(basket)).ReturnsAsync(basket);
            var result = await _controller.RemoveItemFromBasket(basketId, productId);

            result.Should().BeOfType<NoContentResult>();
        }


        [Fact]
        public async Task DeleteBasket_WhenBasketNotFound_ReturnsNotFound()
        {
            string basketId = "basket_99";
            _basketRepoMock.Setup(r => r.GetBasketAsync(basketId)).ReturnsAsync((CustomerBasket?)null);
            var result = await _controller.DeleteBasket(basketId);

            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().BeOfType<ApiResponse>()
                .Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task DeleteBasket_WhenBasketExists_ReturnsNoContent()
        {
            string basketId = "basket_1";
            var basket = new CustomerBasket(basketId);

            _basketRepoMock.Setup(r => r.GetBasketAsync(basketId)).ReturnsAsync(basket);
            _basketRepoMock.Setup(r => r.DeleteBasketAsync(basketId)).ReturnsAsync(true);
            var result = await _controller.DeleteBasket(basketId);

            result.Should().BeOfType<NoContentResult>();
        }
    }
}
