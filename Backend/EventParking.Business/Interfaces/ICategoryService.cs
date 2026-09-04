using EventParking.Models.DTOs.Category;

namespace EventParking.Business.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();

    Task<CategoryDto> GetByIdAsync(int id);

    Task<CategoryDto> CreateAsync(
        CategoryCreateDto dto);

    Task<CategoryDto> UpdateAsync(
        int id,
        CategoryUpdateDto dto);

    Task DeleteAsync(int id);
}