using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
using System.Timers;
using Xunit;
namespace Store.UnitTests
{
    public class CategoryUnitest
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGenericRepository<Category>> _categoryRepoMock;
        private readonly Mock<IGenericRepository<Product>> _productRepoMock;
        private readonly CategoryController _controller;
        private readonly Mock<IMapper> _mapper;
        public CategoryUnitest()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _categoryRepoMock = new Mock<IGenericRepository<Category>>();
            _productRepoMock = new Mock<IGenericRepository<Product>>();

            _unitOfWorkMock.Setup(u => u.Categories).Returns(_categoryRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);
            _mapper = new Mock<IMapper>();
            _controller = new CategoryController(_unitOfWorkMock.Object, _mapper.Object);
        }
        [Fact]
        public async Task GetAll()
        {
            var categorydto = new List<CategoryDto> { new CategoryDto { Id = 1, Name = "Test" } };
            var category = new List<Category> { new Category { Id = 1, Name = "Test" } };
            _categoryRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(category);
            _mapper.Setup(x => x.Map<IReadOnlyList<CategoryDto>>(category)).Returns(categorydto);
            var result = await _controller.GetCategories();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returned = okResult.Value.Should().BeOfType<List<CategoryDto>>().Subject;
            returned.Should().NotBeNull();
            returned[0].Id.Should().Be(1);
            returned[0].Name.Should().Be("Test");
        }
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task DeleteCategory_WithInvalidId_ReturnsBadRequest(int invalidId)
        {
            var result = await _controller.DeleteCategory(invalidId);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DeleteCategory_WhenCategoryNotFound_ReturnsNotFound()
        {
            int categoryId = 5;
            _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync((Category?)null);

            var result = await _controller.DeleteCategory(categoryId);
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().BeOfType<ApiResponse>()
                .Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task DeleteCategory_WhenCategoryHasAssociatedProducts_ReturnsBadRequest()
        {
            int categoryId = 1;
            var existingCategory = new Category { Id = 1, Name = "Category 1" };
            var products = new List<Product>
        {
            new Product { Id = 10, CategoryId = 1, Name = "Product A" }
        };

            _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(existingCategory);
            _productRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

            var result = await _controller.DeleteCategory(categoryId);

            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeOfType<ApiResponse>()
                .Which.Message.Should().Be("Cannot delete this category because it has associated products.");
        }

        [Fact]
        public async Task DeleteCategory_WithValidIdAndNoProducts_ReturnsOkResult()
        {
            int categoryId = 1;
            var existingCategory = new Category { Id = 1, Name = "Category 1" };
            var products = new List<Product>
        {
            new Product { Id = 10, CategoryId = 2, Name = "Product " }
        };

            _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(existingCategory);
            _productRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(products);
            _categoryRepoMock.Setup(r => r.Delete(existingCategory));
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            var result = await _controller.DeleteCategory(categoryId);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be("Category Deleted successfully");
        }

        [Fact]
        public async Task DeleteCategory_WhenSaveFails_ReturnsInternalServerError()
        {
            int categoryId = 1;
            var existingCategory = new Category { Id = 1, Name = "Category 1" };
            var products = new List<Product>(); 

            _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(existingCategory);
            _productRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(products);
            _categoryRepoMock.Setup(r => r.Delete(existingCategory));
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(0);
            var result = await _controller.DeleteCategory(categoryId);

            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }
        [Fact]
        public async Task CreateCategory_WithValidDto_ReturnsOkResult()
        {
            var dto = new CreateCategoryDto { Name = "Electronics", Description = "Devices and gadgets" };
            _categoryRepoMock.Setup(r => r.AddAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1); 

            var result = await _controller.CreateCategory(dto);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be("Category added sucessfully");
        }

        [Fact]
        public async Task CreateCategory_WhenSaveFails_ReturnsInternalServerError()
        {
            var dto = new CreateCategoryDto { Name = "Electronics", Description = "Devices" };
            _categoryRepoMock.Setup(r => r.AddAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(0); 
            var result = await _controller.CreateCategory(dto);

            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }
        [Fact]
        public async Task UpdateCategory_WhenCategoryNotFound_ReturnsNotFound()
        {
            int categoryId = 99;
            var dto = new UpdateCategoryDto { Name = "Updated Name", Description = "Updated Desc" };

            _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync((Category?)null);

            var result = await _controller.UpdateCategory(categoryId, dto);

            var NotFoudResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            NotFoudResult.Value.Should().BeOfType<ApiResponse>()
                .Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task UpdateCategory_WithValidData_ReturnsOkResult()
        {
            int categoryId = 1;
            var existingCategory = new Category { Id = 1, Name = "Old Name", Description = "Old Desc" };
            var dto = new UpdateCategoryDto { Name = "New Name", Description = "New Desc" };

            _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(existingCategory);
            _categoryRepoMock.Setup(r => r.Update(It.IsAny<Category>()));
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1); 
            var result = await _controller.UpdateCategory(categoryId, dto);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be("Category Updated successfully");
        }

        [Fact]
        public async Task UpdateCategory_WhenSaveFails_ReturnsInternalServerError()
        {
            int categoryId = 1;
            var existingCategory = new Category { Id = 1, Name = "Old Name", Description = "Old Desc" };
            var dto = new UpdateCategoryDto { Name = "New Name", Description = "New Desc" };

            _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(existingCategory);
            _categoryRepoMock.Setup(r => r.Update(It.IsAny<Category>()));
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(0); 

            var result = await _controller.UpdateCategory(categoryId, dto);

            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }
    }
}