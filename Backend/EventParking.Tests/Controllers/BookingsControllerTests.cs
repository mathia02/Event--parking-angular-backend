using System.Security.Claims;
using EventParking.Api.Controllers;
using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.Models.DTOs.Booking;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EventParking.Tests.Controllers;

public class BookingsControllerTests
{
    private readonly Mock<IBookingService>
        _bookingServiceMock;

    private readonly BookingsController
        _controller;

    public BookingsControllerTests()
    {
        _bookingServiceMock =
            new Mock<IBookingService>();

        _controller =
            new BookingsController(
                _bookingServiceMock.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk()
    {
        // Act
        var result =
            await _controller.GetAll();

        // Assert
        Assert.IsType<OkObjectResult>(result);

        _bookingServiceMock.Verify(
            x => x.GetAllAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetMyBookings_ShouldReturnOk_WhenCustomerClaimIsValid()
    {
        // Arrange
        SetUser(
            userId: 3,
            role: "Customer");

        // Act
        var result =
            await _controller
                .GetMyBookings();

        // Assert
        Assert.IsType<OkObjectResult>(
            result);

        _bookingServiceMock.Verify(
            x =>
                x.GetMyBookingsAsync(3),
            Times.Once);
    }

    [Fact]
    public async Task GetMyBookings_ShouldReturn403_WhenUserIdClaimIsInvalid()
    {
        // Arrange
        SetInvalidUserId(
            role: "Customer");

        // Act
        var result =
            await _controller
                .GetMyBookings();

        // Assert
        var objectResult =
            Assert.IsType<ObjectResult>(
                result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            objectResult.StatusCode);

        _bookingServiceMock.Verify(
            x =>
                x.GetMyBookingsAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenBookingDoesNotExist()
    {
        // Arrange
        SetUser(
            userId: 3,
            role: "Customer");

        _bookingServiceMock
            .Setup(x =>
                x.GetByIdAsync(
                    999,
                    3,
                    false))
            .ThrowsAsync(
                new NotFoundException(
                    "Booking not found."));

        // Act
        var result =
            await _controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(
            result);
    }

    [Fact]
    public async Task GetById_ShouldReturn403_WhenUserCannotAccessBooking()
    {
        // Arrange
        SetUser(
            userId: 3,
            role: "Customer");

        _bookingServiceMock
            .Setup(x =>
                x.GetByIdAsync(
                    1,
                    3,
                    false))
            .ThrowsAsync(
                new UnauthorizedAccessException(
                    "Access denied."));

        // Act
        var result =
            await _controller.GetById(1);

        // Assert
        var objectResult =
            Assert.IsType<ObjectResult>(
                result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            objectResult.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenValidationFails()
    {
        // Arrange
        SetUser(
            userId: 3,
            role: "Customer");

        var dto =
            new BookingCreateDto();

        _bookingServiceMock
            .Setup(x =>
                x.CreateAsync(
                    3,
                    dto))
            .ThrowsAsync(
                new ValidationException(
                    "Invalid booking."));

        // Act
        var result =
            await _controller.Create(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(
            result);
    }

    [Fact]
    public async Task Create_ShouldReturn409_WhenBookingConflictOccurs()
    {
        // Arrange
        SetUser(
            userId: 3,
            role: "Customer");

        var dto =
            new BookingCreateDto();

        _bookingServiceMock
            .Setup(x =>
                x.CreateAsync(
                    3,
                    dto))
            .ThrowsAsync(
                new ConflictException(
                    "Seat is already reserved."));

        // Act
        var result =
            await _controller.Create(dto);

        // Assert
        Assert.IsType<ConflictObjectResult>(
            result);
    }

    [Fact]
    public async Task Cancel_ShouldReturnOk_ForAdministrator()
    {
        // Arrange
        SetUser(
            userId: 1,
            role: "Administrator");

        // Act
        var result =
            await _controller.Cancel(10);

        // Assert
        Assert.IsType<OkObjectResult>(
            result);

        _bookingServiceMock.Verify(
            x =>
                x.CancelAsync(
                    10,
                    1,
                    true),
            Times.Once);
    }

    private void SetUser(
        int userId,
        string role)
    {
        var claims =
            new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    userId.ToString()),

                new(
                    ClaimTypes.Role,
                    role)
            };

        var identity =
            new ClaimsIdentity(
                claims,
                "TestAuthentication");

        var principal =
            new ClaimsPrincipal(identity);

        _controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext
                    {
                        User = principal
                    }
            };
    }

    private void SetInvalidUserId(
        string role)
    {
        var claims =
            new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    "invalid-id"),

                new(
                    ClaimTypes.Role,
                    role)
            };

        var identity =
            new ClaimsIdentity(
                claims,
                "TestAuthentication");

        var principal =
            new ClaimsPrincipal(identity);

        _controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext
                    {
                        User = principal
                    }
            };
    }
}