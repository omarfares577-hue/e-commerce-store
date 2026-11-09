using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Store.API.Errors;
using Store.Application.Contracts;
using Store.Application.Dtos.CreateDtos;
using Store.Application.Dtos.Identity;
using Store.Application.Dtos.ReturnDtos;
using Store.Application.Dtos.UpdateDtos;
using Store.Domain.Entities;
using Store.Domain.Entities.Identity;
using Store.Infrastructure.Repositories;
using System.Security.Claims;
namespace Store.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public CategoryController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories()
        {
            var category = await _unitOfWork.Categories.GetAllAsync();
            var mappedCategory = _mapper.Map<IReadOnlyList<CategoryDto>>(category);
            return Ok(mappedCategory);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateCategory(CreateCategoryDto Dto)
        {
            var category = new Category
            {
                Name = Dto.Name,
                Description = Dto.Description
            };
            await _unitOfWork.Categories.AddAsync(category);
            var result = await _unitOfWork.CompleteAsync();
            if (result > 0)
                return Ok("Category added sucessfully");
            return StatusCode(500, new ApiResponse(500));
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, UpdateCategoryDto Dto)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category is null)
                return NotFound(new ApiResponse(404));
            category.Name = Dto.Name;
            category.Description = Dto.Description;
            _unitOfWork.Categories.Update(category);
            var result = await _unitOfWork.CompleteAsync();
            if (result > 0)
                return Ok("Category Updated successfully");
            return StatusCode(500, new ApiResponse(500));
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if(id<=0)
                return BadRequest(new ApiResponse(400));

            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category is null)
                return NotFound(new ApiResponse(404));
            var product = await _unitOfWork.Products.GetAllAsync();
            bool hasProducts = product.Any(p => p.CategoryId == id);
            if (hasProducts)
            {
                return BadRequest(new ApiResponse(400, "Cannot delete this category because it has associated products."));
            }
            _unitOfWork.Categories.Delete(category);
            var result = await _unitOfWork.CompleteAsync();
            if (result > 0)
                return Ok("Category Deleted successfully");
            return StatusCode(500, new ApiResponse(500));
        }
    }
}
