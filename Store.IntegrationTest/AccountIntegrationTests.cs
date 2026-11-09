using FluentAssertions;
using Store.API;
using Store.Application.Dtos.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
namespace Store.IntegrationTest
{
    using FluentAssertions;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.Extensions.DependencyInjection;
    using Store.API;
    using Store.Domain.Entities.Identity;
    using System.Net;
    using System.Net.Http.Json;
    using Xunit;
    using Xunit.Abstractions;

    public class AccountIntegrationTests : IntegrationTestBase
    {
        private const string AdminEmail = "admin@store.com";
        private const string AdminPassword = "Adim#123";
        private const string ClientEmail = "client@store.com";
        private const string ClientPassword = "Password123!";
        private const string InvalidPassword = "WrongPassword123!";
        private const string ValidPhoneNumber = "1234567890";


        public AccountIntegrationTests(CustomWebApplicationFactory<ApiMarker> factory, ITestOutputHelper output)
            : base(factory, output)
        {
        }
        private Task<HttpResponseMessage> FailLoginAsync(string email) =>
         _client.PostAsJsonAsync("/api/Account/login",
        new { Email = email, Password = InvalidPassword });

        private async Task ClearLockoutAsync(string email)
        {
            using var scope = _factory.Services.CreateScope();
            var um = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await um.FindByEmailAsync(email);
            await um.SetLockoutEndDateAsync(user!, DateTimeOffset.UtcNow.AddMinutes(-1));
            await um.ResetAccessFailedCountAsync(user!);
        }
        private static string UniqueEmail() => $"user_{Guid.NewGuid():N}@test.com";

        private async Task<UserDto> LoginAsync(string email, string password)
        {
            var response = await _client.PostAsJsonAsync("/api/Account/login", new { Email = email, Password = password });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return (await response.Content.ReadFromJsonAsync<UserDto>())!;
        }

        private Task<HttpResponseMessage> RegisterAsync(string email, string password) =>
            _client.PostAsJsonAsync("/api/Account/register", new { Email = email, Password = password, PhoneNumber = ValidPhoneNumber, Name = "Client", DisplayName = "Client User" });

        private Task<HttpResponseMessage> CreateAdminAsync(string email, string password) =>
            _client.PostAsJsonAsync("/api/Account/create-admin", new { Email = email, Password = password, PhoneNumber = ValidPhoneNumber, Name = "Admin", DisplayName = "Admin User" });
        private Task<HttpResponseMessage> ForgetPasswordAsync(string email) =>
           _client.PostAsJsonAsync("/api/Account/forget-password", new { Email = email });

        private Task<HttpResponseMessage> ResetPasswordAsync(string email, string token, string newPassword, string confirmPassword) =>
            _client.PostAsJsonAsync(
                $"/api/Account/reset-password?email={Uri.EscapeDataString(email)}&token={token}",
                new { NewPassword = newPassword, ConfirmPassword = confirmPassword });

        private async Task<string> GenerateValidResetTokenAsync(string email)
        {
            using var scope = _factory.Services.CreateScope();
            var um = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await um.FindByEmailAsync(email);
            var raw = await um.GeneratePasswordResetTokenAsync(user!);
            return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(raw));
        }

        private async Task BanPermanentlyAsync(string email)
        {
            for (int i = 0; i < 5; i++) await FailLoginAsync(email);
            await ClearLockoutAsync(email);
            for (int i = 0; i < 5; i++) await FailLoginAsync(email);
        }
        [Fact]
        public async Task Register_WhenEmailAlreadyExists_ReturnsBadRequest()
        {
            var email = UniqueEmail();
            await RegisterAsync(email, "Password123!");

            var response = await RegisterAsync(email, "Password123!");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_WithValidData_ReturnsOkAndToken()
        {
            var email = UniqueEmail();
            var response = await RegisterAsync(email, "Password123!");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<UserDto>();
            result.Should().NotBeNull();
            result!.Email.Should().Be(email);
            result.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CreateAdmin_WhenNotAuthenticated_ReturnsUnauthorized()
        {
            var response = await CreateAdminAsync(UniqueEmail(), "Password123!");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateAdmin_WhenAuthenticatedAsClient_ReturnsForbidden()
        {
            var clientLogin = await LoginAsync(ClientEmail, ClientPassword);
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", clientLogin.Token);

            var response = await CreateAdminAsync(UniqueEmail(), "Password123!");
            var errorContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Error Details: {errorContent}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateAdmin_WhenAuthenticatedAsAdmin_ReturnsOkAndToken()
        {
            await AuthenticateAsAdminAsync();
            var newAdminEmail = UniqueEmail();
            var response = await CreateAdminAsync(newAdminEmail, "Password123!");
            var errorContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Error Details: {errorContent}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<UserDto>();
            result.Should().NotBeNull();
            result!.Email.Should().Be(newAdminEmail);
            result.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Login_WhenUserNotFound_ReturnsUnauthorized()
        {
            var response = await _client.PostAsJsonAsync("/api/Account/login", new { Email = "notfound@test.com", Password = "Password123!" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
        {
            var email = UniqueEmail();
            await RegisterAsync(email, "Password123!");

            var response = await _client.PostAsJsonAsync("/api/Account/login", new { Email = email, Password = InvalidPassword });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WithMultipleFailedAttempts_TriggersLockout()
        {
            var email = UniqueEmail();
            await RegisterAsync(email, "Password123!");

            for (int i = 0; i < 4; i++)
            {
                await _client.PostAsJsonAsync("/api/Account/login", new { Email = email, Password = InvalidPassword });
            }

            var response = await _client.PostAsJsonAsync("/api/Account/login", new { Email = email, Password = InvalidPassword });
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("15 minutes");
        }

        [Fact]
        public async Task Login_WithRepeatedFailures_PermanentlyBansAccount()
        {
            var email = UniqueEmail();
            await RegisterAsync(email, "Password123!");

            for (int i = 0; i < 5; i++)
                await FailLoginAsync(email);

            await ClearLockoutAsync(email);
            HttpResponseMessage response = null!;
            for (int i = 0; i < 5; i++)
                response = await FailLoginAsync(email);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("permanently banned");
        }

        [Fact]
        public async Task Login_WhenPermanentlyBanned_ReturnsBadRequest()
        {
            var email = UniqueEmail();
            await RegisterAsync(email, "Password123!");

            await BanPermanentlyAsync(email);
            var response = await _client.PostAsJsonAsync("/api/Account/login",
                new { Email = email, Password = "Password123!" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Permanently Banned");
        }
        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkAndToken()
        {
            var result = await LoginAsync(ClientEmail, ClientPassword);

            result.Should().NotBeNull();
            result.Email.Should().Be(ClientEmail);
            result.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetCurrentUser_WhenNotAuthenticated_ReturnsUnauthorized()
        {
            var response = await _client.GetAsync("/api/Account/currentuser");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetCurrentUser_WhenAuthenticatedAsClient_ReturnsOkAndUserData()
        {
            var loginResult = await LoginAsync(ClientEmail, ClientPassword);
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult.Token);

            var response = await _client.GetAsync("/api/Account/currentuser");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var user = await response.Content.ReadFromJsonAsync<UserDto>();
            user.Should().NotBeNull();
            user!.Email.Should().Be(ClientEmail);
            user.Token.Should().NotBeNullOrEmpty();
        }
        [Fact]
        public async Task ResetPassword_WithValidToken_ResetsPasswordAndAllowsLogin()
        {
            var email = UniqueEmail();
            await RegisterAsync(email, "Password123!");
            var token = await GenerateValidResetTokenAsync(email);

            var response = await ResetPasswordAsync(email, token, "NewPassword123!", "NewPassword123!");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var login = await LoginAsync(email, "NewPassword123!");
            login.Token.Should().NotBeNullOrEmpty();

            var oldLogin = await FailLoginAsync(email);
            oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ResetPassword_WhenPasswordsDoNotMatch_ReturnsBadRequest()
        {
            var email = UniqueEmail();
            await RegisterAsync(email, "Password123!");
            var token = await GenerateValidResetTokenAsync(email);

            var response = await ResetPasswordAsync(email, token, "NewPassword123!", "Different123!");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().ContainEquivalentOf("do not match");
        }
        [Fact]
        public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
        {
            var email = UniqueEmail();
            await RegisterAsync(email, "Password123!");
            var fakeToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("this-is-not-a-valid-token"));

            var response = await ResetPasswordAsync(email, fakeToken, "NewPassword123!", "NewPassword123!");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ResetPassword_WithMalformedToken_ReturnsBadRequest()
        {
            var email = UniqueEmail();
            await RegisterAsync(email, "Password123!");

            var response = await ResetPasswordAsync(email, "!!!not-base64!!!", "NewPassword123!", "NewPassword123!");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().Contain("Invalid Reset Link");
        }

        [Fact]
        public async Task ResetPassword_WhenUserNotFound_ReturnsBadRequest()
        {
            var token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("whatever"));

            var response = await ResetPasswordAsync("ghost_" + Guid.NewGuid().ToString("N") + "@test.com",
                token, "NewPassword123!", "NewPassword123!");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().Contain("Invalid Request");
        }

        [Fact]
        public async Task ResetPassword_WhenBanned_UnbansAccountAndAllowsLogin()
        {
            var email = UniqueEmail();
            await RegisterAsync(email, "Password123!");
            await BanPermanentlyAsync(email);
            var banned = await _client.PostAsJsonAsync("/api/Account/login",
                new { Email = email, Password = "Password123!" });
            (await banned.Content.ReadAsStringAsync()).Should().Contain("Permanently Banned");

            var token = await GenerateValidResetTokenAsync(email);
            var response = await ResetPasswordAsync(email, token, "NewPassword123!", "NewPassword123!");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var login = await LoginAsync(email, "NewPassword123!");
            login.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ResetPassword_TokenCannotBeUsedTwice()
        {
            var email = UniqueEmail();
            await RegisterAsync(email, "Password123!");
            var token = await GenerateValidResetTokenAsync(email);

            var first = await ResetPasswordAsync(email, token, "NewPassword123!", "NewPassword123!");
            first.StatusCode.Should().Be(HttpStatusCode.OK);

            var second = await ResetPasswordAsync(email, token, "AnotherPass123!", "AnotherPass123!");
            second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        [Fact]
        public async Task ForgetPassword_WhenEmailExists_ReturnsOk()
        {
            var email = UniqueEmail();
            await RegisterAsync(email, "Password123!");

            var response = await ForgetPasswordAsync(email);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("reset link has been sent");
        }

        [Fact]
        public async Task ForgetPassword_WhenEmailNotFound_ReturnsSameOkMessage()
        {
            var response = await ForgetPasswordAsync("nobody_" + Guid.NewGuid().ToString("N") + "@test.com");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("reset link has been sent");
        }

        [Fact]
        public async Task ForgetPassword_WhenPermanentlyBanned_ReturnsBadRequest()
        {
            var email = UniqueEmail();
            await RegisterAsync(email, "Password123!");
            await BanPermanentlyAsync(email);

            var response = await ForgetPasswordAsync(email);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("permanently banned");
        }
    }
}
