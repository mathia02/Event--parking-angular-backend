using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Category;
using EventParking.Models.Entities;

namespace EventParking.Business.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(
        ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var categories =
            await _categoryRepository.GetAllAsync();

        return categories
            .Select(MapToDto)
            .ToList();
    }

    public async Task<CategoryDto> GetByIdAsync(int id)
    {
        var category =
            await _categoryRepository.GetByIdAsync(id);

        if (category is null)
        {
            throw new NotFoundException(
                $"Category with ID {id} was not found.");
        }

        return MapToDto(category);
    }

    public async Task<CategoryDto> CreateAsync(
        CategoryCreateDto dto)
    {
        ValidateName(dto.Name);

        var name = dto.Name.Trim();

        var nameExists =
            await _categoryRepository
                .NameExistsAsync(name);

        if (nameExists)
        {
            throw new ConflictException(
                "A category with this name already exists.");
        }

        var category = new EventCategory
        {
            Name = name,

            Description =
                string.IsNullOrWhiteSpace(dto.Description)
                    ? null
                    : dto.Description.Trim(),

            CreatedAt = DateTime.UtcNow
        };

        await _categoryRepository.AddAsync(category);

        await _categoryRepository.SaveChangesAsync();

        return MapToDto(category);
    }

    public async Task<CategoryDto> UpdateAsync(
        int id,
        CategoryUpdateDto dto)
    {
        ValidateName(dto.Name);

        var category =
            await _categoryRepository.GetByIdAsync(id);

        if (category is null)
        {
            throw new NotFoundException(
                $"Category with ID {id} was not found.");
        }

        var name = dto.Name.Trim();

        var nameExists =
            await _categoryRepository.NameExistsAsync(
                name,
                id);

        if (nameExists)
        {
            throw new ConflictException(
                "Another category with this name already exists.");
        }

        category.Name = name;

        category.Description =
            string.IsNullOrWhiteSpace(dto.Description)
                ? null
                : dto.Description.Trim();

        category.UpdatedAt = DateTime.UtcNow;

        _categoryRepository.Update(category);

        await _categoryRepository.SaveChangesAsync();

        return MapToDto(category);
    }

    public async Task DeleteAsync(int id)
    {
        var category =
            await _categoryRepository.GetByIdAsync(id);

        if (category is null)
        {
            throw new NotFoundException(
                $"Category with ID {id} was not found.");
        }

        var hasEvents =
            await _categoryRepository
                .HasEventsAsync(id);

        if (hasEvents)
        {
            throw new ConflictException(
                "Category cannot be deleted because it is assigned to one or more events.");
        }

        _categoryRepository.Delete(category);

        await _categoryRepository.SaveChangesAsync();
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException(
                "Category name is required.");
        }
    }

    private static CategoryDto MapToDto(
        EventCategory category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };
    }
}