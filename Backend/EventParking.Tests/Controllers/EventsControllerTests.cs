using EventParking.Api.Controllers;
using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.Models.DTOs.Event;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EventParking.Tests.Controllers;

public class EventsControllerTests
{
    private readonly Mock<IEventService>
        _eventServiceMock;

    private readonly EventsController
        _controller;

    public EventsControllerTests()
    {
        _eventServiceMock =
            new Mock<IEventService>();

        _controller =
            new EventsController(
                _eventServiceMock.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk()
    {
        // Act
        var result =
            await _controller.GetAll(
                null,
                null,
                null,
                null);

        // Assert
        Assert.IsType<OkObjectResult>(
            result);

        _eventServiceMock.Verify(
            x =>
                x.GetAllAsync(
                    null,
                    null,
                    null,
                    null),
            Times.Once);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenEventExists()
    {
        // Act
        var result =
            await _controller.GetById(1);

        // Assert
        Assert.IsType<OkObjectResult>(
            result);

        _eventServiceMock.Verify(
            x => x.GetByIdAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenEventDoesNotExist()
    {
        // Arrange
        _eventServiceMock
            .Setup(x =>
                x.GetByIdAsync(999))
            .ThrowsAsync(
                new NotFoundException(
                    "Event not found."));

        // Act
        var result =
            await _controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(
            result);
    }

    [Fact]
    public async Task Create_ShouldReturn404_WhenRelatedResourceDoesNotExist()
    {
        // Arrange
        var dto =
            new EventCreateDto();

        _eventServiceMock
            .Setup(x =>
                x.CreateAsync(dto))
            .ThrowsAsync(
                new NotFoundException(
                    "Venue not found."));

        // Act
        var result =
            await _controller.Create(dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(
            result);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenValidationFails()
    {
        // Arrange
        var dto =
            new EventCreateDto();

        _eventServiceMock
            .Setup(x =>
                x.CreateAsync(dto))
            .ThrowsAsync(
                new ValidationException(
                    "Invalid event data."));

        // Act
        var result =
            await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(
            result);
    }

    [Fact]
    public async Task Update_ShouldReturn409_WhenScheduleConflictOccurs()
    {
        // Arrange
        var dto =
            new EventUpdateDto();

        _eventServiceMock
            .Setup(x =>
                x.UpdateAsync(
                    1,
                    dto))
            .ThrowsAsync(
                new ConflictException(
                    "Event schedule conflict."));

        // Act
        var result =
            await _controller.Update(
                1,
                dto);

        // Assert
        Assert.IsType<ConflictObjectResult>(
            result);
    }

    [Fact]
    public async Task Delete_ShouldReturnOk_WhenEventIsDeleted()
    {
        // Act
        var result =
            await _controller.Delete(1);

        // Assert
        Assert.IsType<OkObjectResult>(
            result);

        _eventServiceMock.Verify(
            x => x.DeleteAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldReturn409_WhenEventCannotBeDeleted()
    {
        // Arrange
        _eventServiceMock
            .Setup(x =>
                x.DeleteAsync(1))
            .ThrowsAsync(
                new ConflictException(
                    "Event has existing bookings."));

        // Act
        var result =
            await _controller.Delete(1);

        // Assert
        Assert.IsType<ConflictObjectResult>(
            result);
    }
}