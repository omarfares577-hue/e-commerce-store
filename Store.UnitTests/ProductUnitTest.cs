using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Store.API.Controllers;
using Store.API.Errors;
using Store.Application.Contracts;
using Store.Application.Dtos.CreateDtos;
using Store.Application.Dtos.ReturnDtos;
using Store.Application.Dtos.UpdateDtos;
using Store.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Store.Application.Specification;
using Store.API.Helpers;
namespace Store.UnitTests
{
    public class ProductUnitTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGenericRepository<Product>> _productRepoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly ProductsController _controller;

        public ProductUnitTest()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _productRepoMock = new Mock<IGenericRepository<Product>>();
            _mapperMock = new Mock<IMapper>();

            _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

            _controller = new ProductsController(_unitOfWorkMock.Object, _mapperMock.Object);
        }

        [Fact]
        public async Task GetProducts()
        {
            var product = new List<Product> { new Product { Id = 1, Name = "Test" } };
            var productdto = new List<ProductDto> { new ProductDto { Id = 1, Name = "Test" } };
            var specparam = new ProductSpecParam { PageIndex = 1 };
            _productRepoMock.Setup(p => p.GetAllWithSpecAsync(It.IsAny<ISpecification<Product>>())).ReturnsAsync(product);
            _mapperMock.Setup(m => m.Map<IReadOnlyList<ProductDto>>(product)).Returns(productdto);
            _productRepoMock.Setup(p => p.GetAllCountAsync(It.IsAny<ISpecification<Product>>())).ReturnsAsync(1);
            var result = await _controller.GetProducts(specparam);
            var OkResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returned = OkResult.Value.Should().BeOfType<Pagination<ProductDto>>().Subject;
            returned.Data.Should().BeEquivalentTo(productdto);
            returned.TotalCount.Should().Be(1);
            returned.PageIndex.Should().Be(1);
        }
        [Fact]
        public async Task GetProductById_WhenProductExists_ReturnsOkWithProductDto()
        {
            int productId = 1;
            var product = new Product { Id = productId, Name = "Laptop" };
            var productDto = new ProductDto { Id = productId, Name = "Laptop" };

            _productRepoMock.Setup(r => r.GetByIdWithSpecAsync(It.IsAny<ISpecification<Product>>()))
                            .ReturnsAsync(product);
            _mapperMock.Setup(m => m.Map<Product, ProductDto>(product)).Returns(productDto);
            var result = await _controller.GetProduct(productId);

            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedDto = okResult.Value.Should().BeOfType<ProductDto>().Subject;
            returnedDto.Id.Should().Be(productId);
        }

        [Fact]
        public async Task GetProductById_WhenProductNotFound_ReturnsNotFound()
        {
            int productId = 99;
            _productRepoMock.Setup(r => r.GetByIdWithSpecAsync(It.IsAny<ISpecification<Product>>()))
                            .ReturnsAsync((Product?)null);
            var result = await _controller.GetProduct(productId);

            var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().BeOfType<ApiResponse>()
                .Which.StatusCode.Should().Be(404);
        }


        [Fact]
        public async Task AddProduct_WithValidDto_ReturnsOkResult()
        {
            var dto = new CreateProductDto { Name = "Phone", Price = 500, CategoryId = 1 };
            _productRepoMock.Setup(r => r.AddAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            var result = await _controller.AddProduct(dto);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be("Product added sucessfully");
        }

        [Fact]
        public async Task AddProduct_WhenSaveFails_ReturnsInternalServerError()
        {
            var dto = new CreateProductDto { Name = "Phone", Price = 500, CategoryId = 1 };
            _productRepoMock.Setup(r => r.AddAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(0); 
            var result = await _controller.AddProduct(dto);

            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }


        [Fact]
        public async Task UpdateProduct_WhenProductNotFound_ReturnsBadRequest()
        {
            int productId = 10;
            var dto = new UpdateProductDto { Name = "Updated" };
            _productRepoMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((Product?)null);
            var result = await _controller.UpdateProduct(productId, dto);

            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeOfType<ApiResponse>()
                .Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task UpdateProduct_WithValidData_ReturnsOkResult()
        {
            int productId = 1;
            var existingProduct = new Product { Id = productId, Name = "Old Name" };
            var dto = new UpdateProductDto { Name = "New Name", Price = 700 };

            _productRepoMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(existingProduct);
            _productRepoMock.Setup(r => r.Update(existingProduct));
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            var result = await _controller.UpdateProduct(productId, dto);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be("Product Updated successfully");
        }

        [Fact]
        public async Task UpdateProduct_WhenSaveFails_ReturnsInternalServerError()
        {
            int productId = 1;
            var existingProduct = new Product { Id = productId, Name = "Old Name" };
            var dto = new UpdateProductDto { Name = "New Name" };

            _productRepoMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(existingProduct);
            _productRepoMock.Setup(r => r.Update(existingProduct));
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(0); 
            var result = await _controller.UpdateProduct(productId, dto);

            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }


        [Fact]
        public async Task DeleteProduct_WhenProductNotFound_ReturnsBadRequest()
        {
            int productId = 5;
            _productRepoMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((Product?)null);
            var result = await _controller.DeleteProduct(productId);

            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeOfType<ApiResponse>()
                .Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task DeleteProduct_WithValidId_ReturnsOkResult()
        {
            int productId = 1;
            var existingProduct = new Product { Id = productId, Name = "Product to delete" };
            _productRepoMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(existingProduct);
            _productRepoMock.Setup(r => r.Delete(existingProduct));
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            var result = await _controller.DeleteProduct(productId);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be("Product Deleted successfully");
        }

        [Fact]
        public async Task DeleteProduct_WhenSaveFails_ReturnsInternalServerError()
        {
            int productId = 1;
            var existingProduct = new Product { Id = productId, Name = "Product" };

            _productRepoMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(existingProduct);
            _productRepoMock.Setup(r => r.Delete(existingProduct));
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(0); 
            var result = await _controller.DeleteProduct(productId);

            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }
    }
}
