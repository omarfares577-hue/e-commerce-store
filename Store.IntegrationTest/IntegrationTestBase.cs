using Store.API;
using Store.Application.Dtos.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using System.Net.Http.Headers;
using System.Diagnostics;
namespace Store.IntegrationTest
{
    public class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory<ApiMarker>>
    {
        protected readonly HttpClient _client;
        protected readonly CustomWebApplicationFactory<ApiMarker> _factory;
        protected readonly ITestOutputHelper _output;

        public IntegrationTestBase(CustomWebApplicationFactory<ApiMarker> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
            _client = factory.CreateClient();
        }
        protected async Task AuthenticateAsAdminAsync()
        {
            var login = new LoginDto { Email = "admin@store.com", Password = "Admin#123", Remeberme = true };
            var response = await _client.PostAsJsonAsync("/api/Account/login", login);
            if (response.IsSuccessStatusCode)
            {
                var authResult = await response.Content.ReadFromJsonAsync<UserDto>();
                if(authResult!.Token != null)
                {
                    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer" ,authResult.Token);
                }

            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Login Failed {response.StatusCode} - {error}");
            }
        }
        protected async Task AuthenticateAsClientAsync()
        {
            var loginDto = new { Email = "client@store.com", Password = "Password123!" };
            var response = await _client.PostAsJsonAsync("/api/Account/login", loginDto);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"Login Failed: {response.StatusCode} - {errorContent}");
            }
            else
            {
                var authResult = await response.Content.ReadFromJsonAsync<UserDto>();
                if (authResult?.Token != null)
                {
                    _client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", authResult.Token);
                }
            }
        }

    }
}