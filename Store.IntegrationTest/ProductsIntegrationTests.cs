using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Store.API;
using Store.Application.Dtos.CreateDtos;
using Store.Application.Dtos.UpdateDtos;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace Store.IntegrationTest
{

    public class ProductsIntegrationTests : IntegrationTestBase
    {

        public ProductsIntegrationTests(CustomWebApplicationFactory<ApiMarker> factory, ITestOutputHelper output) : base(factory, output)
        {
        }

        [Fact]
        public async Task GetProducts_ReturnsOkWithPagination()
        {
            var response = await _client.GetAsync("/api/Products");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetProductById_WhenIdExists_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/Products/1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetProductById_WhenIdDoesNotExist_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/api/Products/9999");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task AddProduct_WhenNotAuthenticated_ReturnsUnauthorized()
        {
            var dto = new CreateProductDto { Name = "Test", Description = "Test", Price = 10, StockQuantity = 5, CategoryId = 1, PictureUrl = "test.jpg" };
            var response = await _client.PostAsJsonAsync("/api/Products", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task AddProduct_WhenUserIsClientRole_ReturnsForbidden()
        {
            await AuthenticateAsClientAsync();
            var dto = new CreateProductDto { Name = "Test", Description = "Test", Price = 10, StockQuantity = 5, CategoryId = 1, PictureUrl = "test.jpg" };
            var response = await _client.PostAsJsonAsync("/api/Products", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task AddProduct_WhenUserIsAdminRole_ReturnsOk()
        {
            await AuthenticateAsAdminAsync();
            var dto = new CreateProductDto { Name = "New Product", Description = "Desc", Price = 100, StockQuantity = 10, CategoryId = 1, PictureUrl = "img.jpg" };
            var response = await _client.PostAsJsonAsync("/api/Products", dto);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task UpdateProduct_WhenUserIsAdminRoleAndProductExists_ReturnsOk()
        {
            await AuthenticateAsAdminAsync();
            var dto = new UpdateProductDto { Name = "Updated", Description = "Desc", Price = 150, StockQuantity = 5, CategoryId = 1, PictureUrl = "img.jpg" };
            var response = await _client.PutAsJsonAsync("/api/Products/1", dto);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task UpdateProduct_WhenProductDoesNotExist_ReturnsBadRequest()
        {
            await AuthenticateAsAdminAsync();
            var dto = new UpdateProductDto { Name = "Updated", Description = "Desc", Price = 150, StockQuantity = 5, CategoryId = 1, PictureUrl = "img.jpg" };
            var response = await _client.PutAsJsonAsync("/api/Products/9999", dto);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task DeleteProduct_WhenUserIsAdminRoleAndProductExists_ReturnsOk()
        {
            await AuthenticateAsAdminAsync();
            var response = await _client.DeleteAsync("/api/Products/1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task DeleteProduct_WhenProductDoesNotExist_ReturnsBadRequest()
        {
            await AuthenticateAsAdminAsync();
            var response = await _client.DeleteAsync("/api/Products/9999");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
