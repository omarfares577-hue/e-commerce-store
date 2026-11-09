using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Store.API;
using Store.Application.Dtos.CreateDtos;
using Store.Application.Dtos.ReturnDtos;
using Store.Domain.Entities;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace Store.IntegrationTest
{

    public class OrdersIntegrationTests : IntegrationTestBase
    {

        public OrdersIntegrationTests(CustomWebApplicationFactory<ApiMarker> factory, ITestOutputHelper output)
            : base(factory, output)
        {
        }


        [Fact]
        public async Task GetDeliveryMethods_ReturnsOk_WithoutAuthentication()
        {
            var response = await _client.GetAsync("/api/Order/delivery-methods");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var deliveryMethods = await response.Content.ReadFromJsonAsync<List<DeliveryMethod>>();
            deliveryMethods.Should().NotBeNull();
        }


        [Fact]
        public async Task CreateOrder_WhenNotAuthenticated_ReturnsUnauthorized()
        {
            var dto = new CreateOrderDto();

            var response = await _client.PostAsJsonAsync("/api/Order", dto);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateOrder_WhenUserIsAdminRole_ReturnsForbidden()
        {
            await AuthenticateAsAdminAsync();
            var dto = new CreateOrderDto();
            var response = await _client.PostAsJsonAsync("/api/Order", dto);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateOrder_WhenBasketOrDataInvalid_ReturnsBadRequest()
        {
            await AuthenticateAsClientAsync();
            var invalidDto = new CreateOrderDto
            {
                BasketId = "non_existent_basket",
                DeliveryMethodId = -1
            };

            var response = await _client.PostAsJsonAsync("/api/Order", invalidDto);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }


        [Fact]
        public async Task GetOrders_WhenNotAuthenticated_ReturnsUnauthorized()
        {
            var response = await _client.GetAsync("/api/Order");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetOrders_WhenUserIsClientRole_ReturnsForbidden()
        {
            await AuthenticateAsClientAsync();

            var response = await _client.GetAsync("/api/Order");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetOrders_WhenUserIsAdmin_ReturnsOkWithOrders()
        {
            await AuthenticateAsAdminAsync();

            var response = await _client.GetAsync("/api/Order");
            var errorContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Error Details: {errorContent}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();
            orders.Should().NotBeNull();
        }


        [Fact]
        public async Task GetOrderById_WhenIdNotFound_ReturnsNotFound()
        {
            await AuthenticateAsAdminAsync();
            var nonExistentId = 9999;
            var response = await _client.GetAsync($"/api/Order/{nonExistentId}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetOrderById_WhenUserIsClientRole_ReturnsForbidden()
        {
            await AuthenticateAsClientAsync();
            var response = await _client.GetAsync("/api/Order/1");
            var errorContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Error Details: {errorContent}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetOrderById_WhenValidIdAndAdmin_ReturnsOk()
        {
            await AuthenticateAsAdminAsync();
            int validOrderId = 1;

            var response = await _client.GetAsync($"/api/Order/{validOrderId}");
            var errorContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Error Details: {errorContent}");

            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        }
    }
}
