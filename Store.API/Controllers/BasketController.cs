using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.API.Errors;
using Store.Application.Contracts;
using Store.Application.Dtos.CreateDtos;
using Store.Application.Dtos.UpdateDtos;
using Store.Domain.Entities;

namespace Store.API.Controllers
{
    [Authorize(Roles = "Client")]
    [ApiController]
    [Route("api/[controller]")]
    public class BasketController : ControllerBase
    {
        private readonly IBasketRepository _basketRepository;
        private readonly IUnitOfWork _unitofwork;

        public BasketController(IUnitOfWork unitofwork, IBasketRepository basketRepository)
        {
            _unitofwork = unitofwork;
            _basketRepository = basketRepository;
        }
        [HttpGet("{basketid}")]
        public async Task<ActionResult<CustomerBasket>> GetBasketById(string basketid)
        {
            var basket = await _basketRepository.GetBasketAsync(basketid);
            if(basket == null)
                return NotFound(new ApiResponse(404));
            return Ok(basket );
        }
        [HttpPost("{basketid}")]
        public async Task<ActionResult<CustomerBasket>> CreateBasket(string basketid)
        {
            var existingBasket = await _basketRepository.GetBasketAsync(basketid);
            if (existingBasket != null)
            {
                return BadRequest(new { message = "Basket Already Exist" });
            }
            var newBasket = new CustomerBasket( basketid);

            var createdBasket = await _basketRepository.UpdateBasketAsync(newBasket);
            return  Created("Basket Created Successfully",newBasket);
        }
        [HttpPut]
        public async Task<ActionResult<CustomerBasket>> UpdateBasket(UpdateBasketItemDto updateDto)
        {
            var basket = await _basketRepository.GetBasketAsync(updateDto.BasketId);

            if (basket == null)
            {
                basket = new CustomerBasket(updateDto.BasketId);
            }

            foreach (var itemDto in updateDto.Items)
            {
                var product = await _unitofwork.Products.GetByIdAsync(itemDto.Id);
                if (product == null)
                {
                    return NotFound(new { message = $"Product With {itemDto.Id} Not Found" });
                }

                var existingItem = basket.Items.FirstOrDefault(i => i.Id == itemDto.Id);

                if (existingItem != null)
                {
                    existingItem.Quantity = itemDto.Quantity;
                }
                else
                {
                    basket.Items.Add(new BasketItem
                    {
                        Id = product.Id,
                        ProductName = product.Name,
                        Price = product.Price,
                        PictureUrl = product.PictureUrl,
                        Quantity = itemDto.Quantity
                    });
                }
            }

            var updatedBasket = await _basketRepository.UpdateBasketAsync(basket);
            return Ok(updatedBasket);
        }
        [HttpDelete("{basketId}/items/{productId}")]
        public async Task<IActionResult> RemoveItemFromBasket(string basketId, int productId)
        {
            var basket = await _basketRepository.GetBasketAsync(basketId);
            if (basket == null) return NotFound(new ApiResponse(404));


            basket.Items.RemoveAll(i => i.Id == productId);

            var updatedBasket = await _basketRepository.UpdateBasketAsync(basket);
            return NoContent();
        }
        [HttpDelete("{basketId}")]
        public async Task<IActionResult> DeleteBasket(string basketId)
        {
            var basket = await _basketRepository.GetBasketAsync(basketId);
            if (basket == null) return NotFound(new ApiResponse(404));
            var result = await _basketRepository.DeleteBasketAsync(basketId);
            return NoContent();
        }
    }
}