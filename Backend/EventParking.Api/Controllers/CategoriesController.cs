using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.Models.DTOs.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(
        ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    // GET /api/categories
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories =
            await _categoryService.GetAllAsync();

        return Ok(categories);
    }

    // GET /api/categories/1
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var category =
                await _categoryService.GetByIdAsync(id);

            return Ok(category);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
    }

    // POST /api/categories
    [Authorize(Roles = "Administrator")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CategoryCreateDto dto)
    {
        try
        {
            var category =
                await _categoryService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = category.Id
                },
                category);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (ConflictException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // PUT /api/categories/1
    [Authorize(Roles = "Administrator")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] CategoryUpdateDto dto)
    {
        try
        {
            var category =
                await _categoryService.UpdateAsync(
                    id,
                    dto);

            return Ok(category);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (ConflictException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // DELETE /api/categories/1
    [Authorize(Roles = "Administrator")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _categoryService.DeleteAsync(id);

            return Ok(new
            {
                message =
                    "Category deleted successfully."
            });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (ConflictException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }
}