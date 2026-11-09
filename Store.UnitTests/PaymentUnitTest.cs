using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Store.API.Controllers;
using Store.API.Errors;
using Store.Application;
using Store.Application.Contracts;
using Store.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
namespace Store.UnitTests
{
    public class PaymentUnitTest
    {
        private readonly Mock<IPaymentService> _paymentServiceMock;
        private readonly Mock<IEmailservices> _emailServiceMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly PaymentController _controller;

        public PaymentUnitTest()
        {
            _paymentServiceMock = new Mock<IPaymentService>();
            _emailServiceMock = new Mock<IEmailservices>();
            _configMock = new Mock<IConfiguration>();

            _controller = new PaymentController(
                _paymentServiceMock.Object,
                 _configMock.Object,
                _emailServiceMock.Object

            );
        }


        [Fact]
        public async Task CreateOrUpdatePaymentIntent_WhenBasketIsNull_ReturnsBadRequest()
        {
            string basketId = "basket_123";
            _paymentServiceMock.Setup(s => s.CreateOrUpdatePaymentIntent(basketId))
                               .ReturnsAsync((CustomerBasket?)null);
            var result = await _controller.CreateOrUpdatePaymentIntent(basketId);

            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeOfType<ApiResponse>()
                .Which.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task CreateOrUpdatePaymentIntent_WhenBasketIsValid_ReturnsOkWithBasket()
        {
            // Arrange
            string basketId = "basket_123";
            var basket = new CustomerBasket { Id = basketId };

            _paymentServiceMock.Setup(s => s.CreateOrUpdatePaymentIntent(basketId))
                               .ReturnsAsync(basket);
            var result = await _controller.CreateOrUpdatePaymentIntent(basketId);

            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedBasket = okResult.Value.Should().BeOfType<CustomerBasket>().Subject;
            returnedBasket.Id.Should().Be(basketId);
        }
        [Fact]
        public async Task StripeWebhook_WhenSignatureOrPayLoad_InValid()
        {
            var httpContext = new DefaultHttpContext();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("invalid-payload"));
            httpContext.Request.Body = stream;
            httpContext.Request.Headers["Stripe-Signature"] = "invalid-signature";
            _controller.ControllerContext = new ControllerContext
            { HttpContext = httpContext};
            _configMock.Setup(c => c["StripeSettings:WebhookKey"]).Returns("whsec_test");
            var result = await _controller.StripeWebhook();
            var Badrequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            Badrequest.Value.Should().NotBeNull();
        }

    }
}
