using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using Store.API.Errors;
using Store.API.Helpers;
using Store.Application;
using Store.Application.Contracts;
using Store.Application.Dtos.CreateDtos;
using Store.Application.Dtos.Identity;
using Store.Application.Dtos.ReturnDtos;
using Store.Domain.Entities;
using Store.Domain.Entities.Identity;
using Stripe;
using System.Net;
using System.Security.Claims;

namespace Store.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : BaseApiController
    {
        private readonly IPaymentService _paymentService;
        private readonly string _whSecret;

        private readonly IConfiguration _config;
        private readonly IEmailservices _emailService;

        public PaymentController(IPaymentService paymentService, IConfiguration config, IEmailservices emailservices)
        {
            _paymentService = paymentService;
            _whSecret = config["StripeSettings:WhSecret"]!;
            _config = config;
            _emailService = emailservices;
        }

        [Authorize(Roles ="Client")]
        [HttpPost("{basketId}")]
        public async Task<ActionResult<CustomerBasket>> CreateOrUpdatePaymentIntent(string basketId)
        {
            var basket = await _paymentService.CreateOrUpdatePaymentIntent(basketId);

            if (basket == null)
                return BadRequest(new ApiResponse(400, "Problem with your basket"));

            return Ok(basket);
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _config["StripeSettings:WebhookKey"]
                );

                PaymentIntent paymentIntent;
                Store.Domain.Entities.Order order;

                switch (stripeEvent.Type)
                {
                    case "payment_intent.succeeded":
                        paymentIntent = (PaymentIntent)stripeEvent.Data.Object;

                        order = await _paymentService.UpdateOrderPaymentSucceeded(paymentIntent.Id);
                        if (order != null)
                        {
                            try
                            {
                                string userMail = EmailTemplates.GetOrderConfirmationTemplate(order, order.ShippingAddress!.FirstName);
                                string confirmSubject = $"Orde Confirmed No #{order.Id}";
                                await _emailService.SendEmailAsync(order.BuyerEmail, confirmSubject, userMail);
                                string adminEmailTo = _config["EmailSettings:AdminEmail"] ?? "admin@store.com";
                                string adminMailContent = EmailTemplates.GetAdminOrderAlertTemplate(order);
                                string adminSubject = $"Alert New Order No #{order.Id}";
                                await _emailService.SendEmailAsync(adminEmailTo, adminSubject, adminMailContent);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Webhook Email Failed: {ex.Message}");
                            }
                        }
                        break;

                    case "payment_intent.payment_failed":
                        paymentIntent = (PaymentIntent)stripeEvent.Data.Object;
                        order = await _paymentService.UpdateOrderPaymentFailed(paymentIntent.Id);
                        break;
                }

                return new EmptyResult();
            }
            catch
            {
                return BadRequest(new { error = "Invalid request or signature." });
            }
        }
    }
}