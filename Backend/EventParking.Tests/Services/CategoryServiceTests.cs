using EventParking.Business.Exceptions;
using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Category;
using EventParking.Models.Entities;
using Moq;
using Xunit;

namespace EventParking.Tests.Services;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository>
        _categoryRepositoryMock;

    private readonly CategoryService
        _categoryService;

    public CategoryServiceTests()
    {
        _categoryRepositoryMock =
            new Mock<ICategoryRepository>();

        _categoryService =
            new CategoryService(
                _categoryRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnCategories()
    {
        var categories = new List<EventCategory>
        {
            new EventCategory
            {
                Id = 1,
                Name = "Concert",
                Description = "Music events"
            }
        };

        _categoryRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(categories);

        var result =
            await _categoryService.GetAllAsync();

        Assert.Single(result);

        Assert.Equal(
            "Concert",
            result[0].Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ShouldThrow()
    {
        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(99))
            .ReturnsAsync((EventCategory?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () =>
                _categoryService.GetByIdAsync(99));
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateCategory()
    {
        var dto = new CategoryCreateDto
        {
            Name = "Sports",
            Description = "Sports events"
        };

        _categoryRepositoryMock
            .Setup(x =>
                x.NameExistsAsync(
                    "Sports",
                    null))
            .ReturnsAsync(false);

        _categoryRepositoryMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<EventCategory>()))
            .Returns(Task.CompletedTask);

        _categoryRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result =
            await _categoryService.CreateAsync(dto);

        Assert.Equal(
            "Sports",
            result.Name);

        _categoryRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<EventCategory>(
                    c => c.Name == "Sports")),
            Times.Once);

        _categoryRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenNameExists_ShouldThrowConflict()
    {
        var dto = new CategoryCreateDto
        {
            Name = "Concert",
            Description = "Duplicate"
        };

        _categoryRepositoryMock
            .Setup(x =>
                x.NameExistsAsync(
                    "Concert",
                    null))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _categoryService.CreateAsync(dto));

        _categoryRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<EventCategory>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyName_ShouldThrowValidation()
    {
        var dto = new CategoryCreateDto
        {
            Name = "",
            Description = "Invalid"
        };

        await Assert.ThrowsAsync<ValidationException>(
            () =>
                _categoryService.CreateAsync(dto));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCategory()
    {
        var category = new EventCategory
        {
            Id = 1,
            Name = "Concert",
            Description = "Old description"
        };

        var dto = new CategoryUpdateDto
        {
            Name = "Live Concert",
            Description = "New description"
        };

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(category);

        _categoryRepositoryMock
            .Setup(x =>
                x.NameExistsAsync(
                    "Live Concert",
                    1))
            .ReturnsAsync(false);

        _categoryRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result =
            await _categoryService.UpdateAsync(
                1,
                dto);

        Assert.Equal(
            "Live Concert",
            result.Name);

        _categoryRepositoryMock.Verify(
            x => x.Update(category),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenCategoryHasEvents_ShouldThrowConflict()
    {
        var category = new EventCategory
        {
            Id = 1,
            Name = "Concert"
        };

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(category);

        _categoryRepositoryMock
            .Setup(x => x.HasEventsAsync(1))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _categoryService.DeleteAsync(1));

        _categoryRepositoryMock.Verify(
            x => x.Delete(
                It.IsAny<EventCategory>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenCategoryHasNoEvents_ShouldDelete()
    {
        var category = new EventCategory
        {
            Id = 1,
            Name = "Concert"
        };

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(category);

        _categoryRepositoryMock
            .Setup(x => x.HasEventsAsync(1))
            .ReturnsAsync(false);

        _categoryRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        await _categoryService.DeleteAsync(1);

        _categoryRepositoryMock.Verify(
            x => x.Delete(category),
            Times.Once);

        _categoryRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
}