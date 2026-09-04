using EventParking.Business.Exceptions;
using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Venue;
using EventParking.Models.Entities;
using Moq;
using Xunit;

namespace EventParking.Tests.Services;

public class VenueServiceTests
{
    private readonly Mock<IVenueRepository> _venueRepositoryMock;
    private readonly VenueService _venueService;

    public VenueServiceTests()
    {
        _venueRepositoryMock = new Mock<IVenueRepository>();

        _venueService = new VenueService(
            _venueRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnVenues()
    {
        // Arrange
        var venues = new List<Venue>
        {
            new Venue
            {
                Id = 1,
                Name = "Main Hall",
                Address = "Vavuniya",
                Capacity = 500
            }
        };

        _venueRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(venues);

        // Act
        var result = await _venueService.GetAllAsync();

        // Assert
        Assert.Single(result);

        Assert.Equal(
            "Main Hall",
            result[0].Name);

        Assert.Equal(
            500,
            result[0].Capacity);
    }

    [Fact]
    public async Task GetByIdAsync_WhenVenueExists_ShouldReturnVenue()
    {
        // Arrange
        var venue = new Venue
        {
            Id = 1,
            Name = "Main Hall",
            Address = "Vavuniya",
            Capacity = 500
        };

        _venueRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(venue);

        // Act
        var result = await _venueService.GetByIdAsync(1);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Main Hall", result.Name);
        Assert.Equal("Vavuniya", result.Address);
        Assert.Equal(500, result.Capacity);
    }

    [Fact]
    public async Task GetByIdAsync_WhenVenueDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        _venueRepositoryMock
            .Setup(x => x.GetByIdAsync(99))
            .ReturnsAsync((Venue?)null);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _venueService.GetByIdAsync(99));
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateVenue()
    {
        // Arrange
        var dto = new VenueCreateDto
        {
            Name = "Conference Hall",
            Address = "Vavuniya",
            Capacity = 300
        };

        Venue? savedVenue = null;

        _venueRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Venue>()))
            .Callback<Venue>(venue =>
            {
                savedVenue = venue;
                venue.Id = 1;
            })
            .Returns(Task.CompletedTask);

        _venueRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _venueService.CreateAsync(dto);

        // Assert
        Assert.NotNull(savedVenue);

        Assert.Equal(
            "Conference Hall",
            savedVenue!.Name);

        Assert.Equal(
            "Vavuniya",
            savedVenue.Address);

        Assert.Equal(
            300,
            savedVenue.Capacity);

        Assert.Equal(
            "Conference Hall",
            result.Name);

        _venueRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Venue>()),
            Times.Once);

        _venueRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenCapacityIsZero_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new VenueCreateDto
        {
            Name = "Invalid Hall",
            Address = "Vavuniya",
            Capacity = 0
        };

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _venueService.CreateAsync(dto));

        _venueRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Venue>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldUpdateVenue()
    {
        // Arrange
        var venue = new Venue
        {
            Id = 1,
            Name = "Old Hall",
            Address = "Old Address",
            Capacity = 200
        };

        var dto = new VenueUpdateDto
        {
            Name = "Updated Hall",
            Address = "Updated Address",
            Capacity = 600
        };

        _venueRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(venue);

        _venueRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _venueService.UpdateAsync(
            1,
            dto);

        // Assert
        Assert.Equal(
            "Updated Hall",
            result.Name);

        Assert.Equal(
            "Updated Address",
            result.Address);

        Assert.Equal(
            600,
            result.Capacity);

        _venueRepositoryMock.Verify(
            x => x.Update(venue),
            Times.Once);

        _venueRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenVenueDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var dto = new VenueUpdateDto
        {
            Name = "Updated Hall",
            Address = "Vavuniya",
            Capacity = 600
        };

        _venueRepositoryMock
            .Setup(x => x.GetByIdAsync(99))
            .ReturnsAsync((Venue?)null);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _venueService.UpdateAsync(
                99,
                dto));
    }

    [Fact]
    public async Task DeleteAsync_WhenUpcomingEventsExist_ShouldThrowConflictException()
    {
        // Arrange
        var venue = new Venue
        {
            Id = 1,
            Name = "Main Hall",
            Address = "Vavuniya",
            Capacity = 500
        };

        _venueRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(venue);

        _venueRepositoryMock
            .Setup(x =>
                x.HasUpcomingEventsAsync(
                    1,
                    It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        // Act + Assert
        await Assert.ThrowsAsync<ConflictException>(
            () => _venueService.DeleteAsync(1));

        _venueRepositoryMock.Verify(
            x => x.Delete(It.IsAny<Venue>()),
            Times.Never);

        _venueRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenNoUpcomingEvents_ShouldDeleteVenue()
    {
        // Arrange
        var venue = new Venue
        {
            Id = 1,
            Name = "Main Hall",
            Address = "Vavuniya",
            Capacity = 500
        };

        _venueRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(venue);

        _venueRepositoryMock
            .Setup(x =>
                x.HasUpcomingEventsAsync(
                    1,
                    It.IsAny<DateTime>()))
            .ReturnsAsync(false);

        _venueRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _venueService.DeleteAsync(1);

        // Assert
        _venueRepositoryMock.Verify(
            x => x.Delete(venue),
            Times.Once);

        _venueRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task IsAvailableAsync_WhenVenueIsFree_ShouldReturnTrue()
    {
        // Arrange
        var venue = new Venue
        {
            Id = 1,
            Name = "Main Hall",
            Address = "Vavuniya",
            Capacity = 500
        };

        var startDateTime =
            new DateTime(
                2026,
                9,
                10,
                10,
                0,
                0);

        var endDateTime =
            new DateTime(
                2026,
                9,
                10,
                12,
                0,
                0);

        _venueRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(venue);

        _venueRepositoryMock
            .Setup(x =>
                x.IsAvailableAsync(
                    1,
                    startDateTime,
                    endDateTime,
                    null))
            .ReturnsAsync(true);

        // Act
        var result =
            await _venueService.IsAvailableAsync(
                1,
                startDateTime,
                endDateTime);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsAvailableAsync_WithInvalidTimeRange_ShouldThrowValidationException()
    {
        // Arrange
        var startDateTime =
            new DateTime(
                2026,
                9,
                10,
                12,
                0,
                0);

        var endDateTime =
            new DateTime(
                2026,
                9,
                10,
                10,
                0,
                0);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _venueService.IsAvailableAsync(
                1,
                startDateTime,
                endDateTime));

        _venueRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<int>()),
            Times.Never);
    }
}