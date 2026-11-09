using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Store.API.Errors;
using Store.Application.Contracts;
using Store.Application.Specification;
using Store.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using Store.Infrastructure.Data;
using Store.Infrastructure.Repositories;
using Store.Application.Mapping;
using Store.API.Helpers;
using System.Linq;
using Microsoft.AspNetCore.Http.HttpResults;
using Store.Application.Dtos.ReturnDtos;
using Store.Application.Dtos.CreateDtos;
using Store.Application.Dtos.UpdateDtos;
using Microsoft.AspNetCore.Authorization;
namespace Store.API.Controllers
{
    public class ProductsController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public ProductsController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        [HttpGet]
        public async Task<ActionResult<Pagination<Product>>> GetProducts([FromQuery] ProductSpecParam specParams)
        {
                var spec = new ProductsWithCategoriesSpecification(specParams);
                var products = await _unitOfWork.Products.GetAllWithSpecAsync(spec);
                var totalItems = await _unitOfWork.Products.GetAllCountAsync(spec);

                var data = _mapper.Map<IReadOnlyList<ProductDto>>(products);

                return Ok(new Pagination<ProductDto>(specParams.PageIndex, specParams.PageSize, totalItems, data));
            
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct([Range(1, int.MaxValue, ErrorMessage = "Product ID must be a positive integer")] int id)
        {
            var spec = new ProductsWithCategoriesSpecification( id);
            var product = await _unitOfWork.Products.GetByIdWithSpecAsync(spec);

            if (product is null)
                return NotFound(new ApiResponse(404));

            var mappedProduct = _mapper.Map<Product, ProductDto>(product);

            return Ok( mappedProduct);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddProduct(CreateProductDto Dto)
        {
              
            var product = new Product
            {
                Name = Dto.Name,
                Description = Dto.Description,
                Price = Dto.Price,
                StockQuantity = Dto.StockQuantity,
                PictureUrl = Dto.PictureUrl,
                CategoryId = Dto.CategoryId
            };
            await _unitOfWork.Products.AddAsync(product);
            var result= await _unitOfWork.CompleteAsync();
            if (result > 0)
                return Ok("Product added sucessfully");
            return StatusCode(500,new ApiResponse(500));
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, UpdateProductDto Dto)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product is null)
                return BadRequest(new ApiResponse(404));
            product.Name = Dto.Name;
            product.Description = Dto.Description;
            product.Price = Dto.Price;
            product.StockQuantity = Dto.StockQuantity;
            product.PictureUrl = Dto.PictureUrl;
            product.CategoryId = Dto.CategoryId;
            _unitOfWork.Products.Update(product);
            var result = await _unitOfWork.CompleteAsync();
            if (result > 0)
                return Ok("Product Updated successfully");
            return StatusCode(500, new ApiResponse(500));
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product is null)
                return BadRequest(new ApiResponse(404));
            _unitOfWork.Products.Delete(product);
            var result = await _unitOfWork.CompleteAsync();
            if (result > 0)
                return Ok("Product Deleted successfully");
            return StatusCode(500, new ApiResponse(500));
        }
    }
}
