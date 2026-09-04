using EventParking.Business.Exceptions;
using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Event;
using EventParking.Models.Entities;
using Moq;
using Xunit;

using EventEntity = EventParking.Models.Entities.Event;

namespace EventParking.Tests.Services;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IVenueRepository> _venueRepositoryMock;
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;

    private readonly EventService _eventService;

    public EventServiceTests()
    {
        _eventRepositoryMock =
            new Mock<IEventRepository>();

        _venueRepositoryMock =
            new Mock<IVenueRepository>();

        _categoryRepositoryMock =
            new Mock<ICategoryRepository>();

        // Our service uses a transaction wrapper.
        // For unit tests, execute the supplied operation directly.
        _eventRepositoryMock
            .Setup(x =>
                x.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(
                async operation =>
                    await operation());

        _eventService = new EventService(
            _eventRepositoryMock.Object,
            _venueRepositoryMock.Object,
            _categoryRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEvents()
    {
        // Arrange
        var events = new List<EventEntity>
        {
            CreateExistingEvent()
        };

        _eventRepositoryMock
            .Setup(x =>
                x.GetAllAsync(
                    null,
                    null,
                    null,
                    null))
            .ReturnsAsync(events);

        // Act
        var result =
            await _eventService.GetAllAsync(
                null,
                null,
                null,
                null);

        // Assert
        Assert.Single(result);

        Assert.Equal(
            "Tech Conference 2026",
            result[0].Name);

        Assert.Equal(
            "Main Hall",
            result[0].VenueName);

        Assert.Equal(
            "Conference",
            result[0].CategoryName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEventExists_ShouldReturnEvent()
    {
        // Arrange
        var eventEntity =
            CreateExistingEvent();

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        // Act
        var result =
            await _eventService.GetByIdAsync(1);

        // Assert
        Assert.Equal(1, result.Id);

        Assert.Equal(
            "Tech Conference 2026",
            result.Name);

        Assert.Equal(
            300,
            result.Capacity);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEventDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(99))
            .ReturnsAsync((EventEntity?)null);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () =>
                _eventService.GetByIdAsync(99));
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateEvent()
    {
        // Arrange
        var dto =
            CreateValidCreateDto();

        var venue = new Venue
        {
            Id = 1,
            Name = "Main Hall",
            Address = "Vavuniya",
            Capacity = 500
        };

        var category = new EventCategory
        {
            Id = 1,
            Name = "Conference"
        };

        _venueRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(venue);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(category);

        _eventRepositoryMock
            .Setup(x =>
                x.HasOverlapAsync(
                    1,
                    dto.StartDateTime,
                    dto.EndDateTime,
                    null))
            .ReturnsAsync(false);

        _eventRepositoryMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<EventEntity>()))
            .Callback<EventEntity>(
                eventEntity =>
                    eventEntity.Id = 1)
            .Returns(Task.CompletedTask);

        _eventRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result =
            await _eventService.CreateAsync(dto);

        // Assert
        Assert.Equal(
            "Tech Conference 2026",
            result.Name);

        Assert.Equal(
            1,
            result.VenueId);

        Assert.Equal(
            1,
            result.CategoryId);

        Assert.Equal(
            300,
            result.Capacity);

        Assert.Equal(
            2500,
            result.TicketPrice);

        _eventRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<EventEntity>()),
            Times.Once);

        _eventRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenVenueDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var dto =
            CreateValidCreateDto();

        _venueRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync((Venue?)null);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () =>
                _eventService.CreateAsync(dto));

        _eventRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<EventEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenCapacityExceedsVenueCapacity_ShouldThrowValidationException()
    {
        // Arrange
        var dto =
            CreateValidCreateDto();

        dto.Capacity = 600;

        var venue = new Venue
        {
            Id = 1,
            Name = "Main Hall",
            Capacity = 500
        };

        var category = new EventCategory
        {
            Id = 1,
            Name = "Conference"
        };

        _venueRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(venue);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(category);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            () =>
                _eventService.CreateAsync(dto));

        _eventRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<EventEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenVenueHasOverlap_ShouldThrowConflictException()
    {
        // Arrange
        var dto =
            CreateValidCreateDto();

        var venue = new Venue
        {
            Id = 1,
            Name = "Main Hall",
            Capacity = 500
        };

        var category = new EventCategory
        {
            Id = 1,
            Name = "Conference"
        };

        _venueRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(venue);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(category);

        _eventRepositoryMock
            .Setup(x =>
                x.HasOverlapAsync(
                    1,
                    dto.StartDateTime,
                    dto.EndDateTime,
                    null))
            .ReturnsAsync(true);

        // Act + Assert
        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _eventService.CreateAsync(dto));

        _eventRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<EventEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenCapacityBelowBookedSeatCount_ShouldThrowValidationException()
    {
        // Arrange
        var eventEntity =
            CreateExistingEvent();

        var dto =
            CreateValidUpdateDto();

        dto.Capacity = 5;

        SetupUpdateDependencies(
            eventEntity);

        _eventRepositoryMock
            .Setup(x =>
                x.GetBookedSeatCountAsync(1))
            .ReturnsAsync(10);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            () =>
                _eventService.UpdateAsync(
                    1,
                    dto));
    }

    [Fact]
    public async Task UpdateAsync_WhenBookingsExistAndTicketPriceChanges_ShouldThrowConflictException()
    {
        // Arrange
        var eventEntity =
            CreateExistingEvent();

        var dto =
            CreateValidUpdateDto();

        dto.TicketPrice = 9999;

        SetupUpdateDependencies(
            eventEntity);

        _eventRepositoryMock
            .Setup(x =>
                x.GetBookedSeatCountAsync(1))
            .ReturnsAsync(0);

        _eventRepositoryMock
            .Setup(x =>
                x.HasAnyBookingsAsync(1))
            .ReturnsAsync(true);

        // Act + Assert
        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _eventService.UpdateAsync(
                    1,
                    dto));
    }

    [Fact]
    public async Task UpdateAsync_WhenVenueHasOverlap_ShouldThrowConflictException()
    {
        // Arrange
        var eventEntity =
            CreateExistingEvent();

        var dto =
            CreateValidUpdateDto();

        SetupUpdateDependencies(
            eventEntity);

        _eventRepositoryMock
            .Setup(x =>
                x.GetBookedSeatCountAsync(1))
            .ReturnsAsync(0);

        _eventRepositoryMock
            .Setup(x =>
                x.HasAnyBookingsAsync(1))
            .ReturnsAsync(false);

        _eventRepositoryMock
            .Setup(x =>
                x.HasOverlapAsync(
                    1,
                    dto.StartDateTime,
                    dto.EndDateTime,
                    1))
            .ReturnsAsync(true);

        // Act + Assert
        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _eventService.UpdateAsync(
                    1,
                    dto));
    }

    [Fact]
    public async Task DeleteAsync_WhenActiveBookingsExist_ShouldThrowConflictException()
    {
        // Arrange
        var eventEntity =
            CreateExistingEvent();

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        _eventRepositoryMock
            .Setup(x =>
                x.HasActiveBookingsAsync(1))
            .ReturnsAsync(true);

        // Act + Assert
        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _eventService.DeleteAsync(1));

        _eventRepositoryMock.Verify(
            x => x.Delete(
                It.IsAny<EventEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenNoActiveBookings_ShouldDeleteEvent()
    {
        // Arrange
        var eventEntity =
            CreateExistingEvent();

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        _eventRepositoryMock
            .Setup(x =>
                x.HasActiveBookingsAsync(1))
            .ReturnsAsync(false);

        _eventRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _eventService.DeleteAsync(1);

        // Assert
        _eventRepositoryMock.Verify(
            x => x.Delete(eventEntity),
            Times.Once);

        _eventRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    private void SetupUpdateDependencies(
        EventEntity eventEntity)
    {
        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        _venueRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(
                new Venue
                {
                    Id = 1,
                    Name = "Main Hall",
                    Address = "Vavuniya",
                    Capacity = 500
                });

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(
                new EventCategory
                {
                    Id = 1,
                    Name = "Conference"
                });
    }

    private static EventCreateDto
        CreateValidCreateDto()
    {
        return new EventCreateDto
        {
            Name =
                "Tech Conference 2026",

            Description =
                "Annual technology conference",

            VenueId = 1,

            CategoryId = 1,

            StartDateTime =
                new DateTime(
                    2026,
                    9,
                    10,
                    10,
                    0,
                    0),

            EndDateTime =
                new DateTime(
                    2026,
                    9,
                    10,
                    12,
                    0,
                    0),

            TicketPrice = 2500,

            ParkingFee = 500,

            Capacity = 300
        };
    }

    private static EventUpdateDto
        CreateValidUpdateDto()
    {
        return new EventUpdateDto
        {
            Name =
                "Tech Conference 2026",

            Description =
                "Updated technology conference",

            VenueId = 1,

            CategoryId = 1,

            StartDateTime =
                new DateTime(
                    2026,
                    9,
                    10,
                    10,
                    0,
                    0),

            EndDateTime =
                new DateTime(
                    2026,
                    9,
                    10,
                    12,
                    0,
                    0),

            TicketPrice = 2500,

            ParkingFee = 500,

            Capacity = 300
        };
    }

    private static EventEntity
        CreateExistingEvent()
    {
        return new EventEntity
        {
            Id = 1,

            Name =
                "Tech Conference 2026",

            Description =
                "Annual technology conference",

            VenueId = 1,

            CategoryId = 1,

            Venue = new Venue
            {
                Id = 1,
                Name = "Main Hall",
                Address = "Vavuniya",
                Capacity = 500
            },

            Category = new EventCategory
            {
                Id = 1,
                Name = "Conference"
            },

            StartDateTime =
                new DateTime(
                    2026,
                    9,
                    10,
                    10,
                    0,
                    0),

            EndDateTime =
                new DateTime(
                    2026,
                    9,
                    10,
                    12,
                    0,
                    0),

            TicketPrice = 2500,

            ParkingFee = 500,

            Capacity = 300
        };
    }
}