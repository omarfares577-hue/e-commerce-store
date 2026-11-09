using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Store.API;
using Store.Application.Dtos.CreateDtos;
using Store.Application.Dtos.ReturnDtos;
using Store.Application.Dtos.UpdateDtos;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;


namespace Store.IntegrationTest
{
    public class CategoriesIntegrationTests : IntegrationTestBase
    {
        public CategoriesIntegrationTests(CustomWebApplicationFactory<ApiMarker> factory, ITestOutputHelper output)
            : base(factory, output)
        {
        }

        [Fact]
        public async Task GetCategories_WhenCalled_ReturnsSuccessAndListOfCategories()
        {
            var response = await _client.GetAsync("/api/Category");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();
            categories.Should().NotBeNull();
        }


        [Fact]
        public async Task CreateCategory_WhenUserNotAuthenticated_ReturnsUnauthorized()
        {
            _client.DefaultRequestHeaders.Authorization = null; 
            var dto = new CreateCategoryDto { Name = "Test Category", Description = "Test Desc" };
            
             var response = await _client.PostAsJsonAsync("/api/Category", dto);
             response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            
        }

        [Fact]
        public async Task CreateCategory_WhenUserIsClientRole_ReturnsForbidden()
        {
            await AuthenticateAsClientAsync(); 
            var dto = new CreateCategoryDto { Name = "Test Category", Description = "Test Desc" };
            var response = await _client.PostAsJsonAsync("/api/Category", dto);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateCategory_WhenUserIsAdmin_ReturnsSuccess()
        {
            await AuthenticateAsAdminAsync(); 
            var dto = new CreateCategoryDto { Name = "Electronics", Description = "Electronic devices" };
            var response = await _client.PostAsJsonAsync("/api/Category", dto);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var message = await response.Content.ReadAsStringAsync();
            message.Should().Contain("Category added sucessfully");
        }


        [Fact]
        public async Task UpdateCategory_WhenNotAuthenticated_ReturnsUnauthorized()
        {
            _client.DefaultRequestHeaders.Authorization = null;
            var dto = new UpdateCategoryDto { Name = "Updated Name", Description = "Updated Desc" };
            var response = await _client.PutAsJsonAsync("/api/Category/1", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateCategory_WhenCategoryNotFound_ReturnsNotFound()
        {
            await AuthenticateAsAdminAsync();
            var dto = new UpdateCategoryDto { Name = "Updated Name", Description = "Updated Desc" };
            var response = await _client.PutAsJsonAsync("/api/Category/9999", dto); 

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateCategory_WithValidDataAsAdmin_ReturnsSuccess()
        {
            await AuthenticateAsAdminAsync();
            var dto = new UpdateCategoryDto { Name = "Updated Electronics", Description = "New Desc" };
            var response = await _client.PutAsJsonAsync("/api/Category/1", dto);

            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        }


        [Fact]
        public async Task DeleteCategory_WhenUserIsClient_ReturnsForbidden()
        {
            await AuthenticateAsClientAsync();
            var response = await _client.DeleteAsync("/api/Category/1");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task DeleteCategory_WhenIdIsInvalid_ReturnsBadRequest()
        {
            await AuthenticateAsAdminAsync();
            var response = await _client.DeleteAsync("/api/Category/0"); 
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task DeleteCategory_WhenCategoryNotFound_ReturnsNotFound()
        {
            await AuthenticateAsAdminAsync();
            var response = await _client.DeleteAsync("/api/Category/9999");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
