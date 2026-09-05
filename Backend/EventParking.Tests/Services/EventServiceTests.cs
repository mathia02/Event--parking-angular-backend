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
    private readonly Mock<IEventRepository>
        _eventRepositoryMock;

    private readonly Mock<IVenueRepository>
        _venueRepositoryMock;

    private readonly Mock<ICategoryRepository>
        _categoryRepositoryMock;

    private readonly EventService
        _eventService;

    public EventServiceTests()
    {
        _eventRepositoryMock =
            new Mock<IEventRepository>();

        _venueRepositoryMock =
            new Mock<IVenueRepository>();

        _categoryRepositoryMock =
            new Mock<ICategoryRepository>();

        _eventRepositoryMock
            .Setup(x =>
                x.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(
                async operation =>
                    await operation());

        _eventService =
            new EventService(
                _eventRepositoryMock.Object,
                _venueRepositoryMock.Object,
                _categoryRepositoryMock.Object);
    }

    [Fact]
    public async Task
        GetAllAsync_ShouldReturnEvents()
    {
        var events =
            new List<EventEntity>
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

        var result =
            await _eventService
                .GetAllAsync(
                    null,
                    null,
                    null,
                    null);

        Assert.Single(result);

        Assert.Equal(
            "Tech Conference",
            result[0].Name);

        Assert.Equal(
            "Main Hall",
            result[0].VenueName);
    }

    [Fact]
    public async Task
        GetByIdAsync_WhenEventExists_ShouldReturnEvent()
    {
        var eventEntity =
            CreateExistingEvent();

        _eventRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        var result =
            await _eventService
                .GetByIdAsync(1);

        Assert.Equal(
            1,
            result.Id);

        Assert.Equal(
            "Tech Conference",
            result.Name);

        Assert.Equal(
            300,
            result.Capacity);
    }

    [Fact]
    public async Task
        GetByIdAsync_WhenEventDoesNotExist_ShouldThrowNotFoundException()
    {
        _eventRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(99))
            .ReturnsAsync(
                (EventEntity?)null);

        await Assert
            .ThrowsAsync<NotFoundException>(
                () =>
                    _eventService
                        .GetByIdAsync(99));
    }

    [Fact]
    public async Task
        CreateAsync_WithValidData_ShouldCreateEvent()
    {
        var dto =
            CreateValidCreateDto();

        SetupVenueAndCategory();

        _eventRepositoryMock
            .Setup(x =>
                x.HasOverlapAsync(
                    dto.VenueId,
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
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result =
            await _eventService
                .CreateAsync(dto);

        Assert.Equal(
            "Tech Conference",
            result.Name);

        Assert.Equal(
            300,
            result.Capacity);

        Assert.Equal(
            2500m,
            result.TicketPrice);

        _eventRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<EventEntity>()),
            Times.Once);
    }

    // =========================================================
    // STEP 08
    // PAST EVENT VALIDATION
    // =========================================================

    [Fact]
    public async Task
        CreateAsync_WhenStartDateIsPast_ShouldThrowValidationException()
    {
        var dto =
            CreateValidCreateDto();

        dto.StartDateTime =
            DateTime.UtcNow
                .AddHours(-2);

        dto.EndDateTime =
            DateTime.UtcNow
                .AddHours(-1);

        var exception =
            await Assert
                .ThrowsAsync<
                    ValidationException>(
                    () =>
                        _eventService
                            .CreateAsync(dto));

        Assert.Contains(
            "future",
            exception.Message,
            StringComparison
                .OrdinalIgnoreCase);

        _eventRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<EventEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task
        UpdateAsync_WhenStartDateIsPast_ShouldThrowValidationException()
    {
        var dto =
            CreateValidUpdateDto();

        dto.StartDateTime =
            DateTime.UtcNow
                .AddDays(-2);

        dto.EndDateTime =
            DateTime.UtcNow
                .AddDays(-2)
                .AddHours(2);

        await Assert
            .ThrowsAsync<
                ValidationException>(
                    () =>
                        _eventService
                            .UpdateAsync(
                                1,
                                dto));
    }

    [Fact]
    public async Task
        CreateAsync_WhenCapacityExceedsVenue_ShouldThrowValidationException()
    {
        var dto =
            CreateValidCreateDto();

        dto.Capacity = 600;

        SetupVenueAndCategory(
            venueCapacity: 500);

        await Assert
            .ThrowsAsync<
                ValidationException>(
                    () =>
                        _eventService
                            .CreateAsync(dto));
    }

    [Fact]
    public async Task
        CreateAsync_WhenVenueOverlaps_ShouldThrowConflictException()
    {
        var dto =
            CreateValidCreateDto();

        SetupVenueAndCategory();

        _eventRepositoryMock
            .Setup(x =>
                x.HasOverlapAsync(
                    dto.VenueId,
                    dto.StartDateTime,
                    dto.EndDateTime,
                    null))
            .ReturnsAsync(true);

        await Assert
            .ThrowsAsync<
                ConflictException>(
                    () =>
                        _eventService
                            .CreateAsync(dto));
    }

    // =========================================================
    // STEP 07
    // SEAT MAP / CAPACITY CONSISTENCY
    // =========================================================

    [Fact]
    public async Task
        UpdateAsync_WhenSeatMapExistsAndCapacityChanges_ShouldThrowConflictException()
    {
        var eventEntity =
            CreateExistingEvent();

        var dto =
            CreateValidUpdateDto();

        dto.Capacity = 250;

        SetupUpdateDependencies(
            eventEntity);

        _eventRepositoryMock
            .Setup(x =>
                x.GetSeatCountAsync(1))
            .ReturnsAsync(300);

        var exception =
            await Assert
                .ThrowsAsync<
                    ConflictException>(
                    () =>
                        _eventService
                            .UpdateAsync(
                                1,
                                dto));

        Assert.Contains(
            "seat map",
            exception.Message,
            StringComparison
                .OrdinalIgnoreCase);
    }

    [Fact]
    public async Task
        UpdateAsync_WhenSeatMapCapacityMatches_ShouldUpdateEvent()
    {
        var eventEntity =
            CreateExistingEvent();

        var dto =
            CreateValidUpdateDto();

        SetupUpdateDependencies(
            eventEntity);

        _eventRepositoryMock
            .Setup(x =>
                x.GetSeatCountAsync(1))
            .ReturnsAsync(300);

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
                    dto.VenueId,
                    dto.StartDateTime,
                    dto.EndDateTime,
                    1))
            .ReturnsAsync(false);

        _eventRepositoryMock
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result =
            await _eventService
                .UpdateAsync(
                    1,
                    dto);

        Assert.Equal(
            300,
            result.Capacity);

        Assert.NotNull(
            eventEntity.UpdatedAt);

        _eventRepositoryMock.Verify(
            x =>
                x.Update(eventEntity),
            Times.Once);
    }

    [Fact]
    public async Task
        UpdateAsync_WhenCapacityBelowBookedSeatCount_ShouldThrowValidationException()
    {
        var eventEntity =
            CreateExistingEvent();

        var dto =
            CreateValidUpdateDto();

        dto.Capacity = 5;

        SetupUpdateDependencies(
            eventEntity);

        _eventRepositoryMock
            .Setup(x =>
                x.GetSeatCountAsync(1))
            .ReturnsAsync(0);

        _eventRepositoryMock
            .Setup(x =>
                x.GetBookedSeatCountAsync(1))
            .ReturnsAsync(10);

        await Assert
            .ThrowsAsync<
                ValidationException>(
                    () =>
                        _eventService
                            .UpdateAsync(
                                1,
                                dto));
    }

    [Fact]
    public async Task
        UpdateAsync_WhenBookingsExistAndTicketPriceChanges_ShouldThrowConflictException()
    {
        var eventEntity =
            CreateExistingEvent();

        var dto =
            CreateValidUpdateDto();

        dto.TicketPrice =
            9999m;

        SetupUpdateDependencies(
            eventEntity);

        _eventRepositoryMock
            .Setup(x =>
                x.GetSeatCountAsync(1))
            .ReturnsAsync(0);

        _eventRepositoryMock
            .Setup(x =>
                x.GetBookedSeatCountAsync(1))
            .ReturnsAsync(0);

        _eventRepositoryMock
            .Setup(x =>
                x.HasAnyBookingsAsync(1))
            .ReturnsAsync(true);

        await Assert
            .ThrowsAsync<
                ConflictException>(
                    () =>
                        _eventService
                            .UpdateAsync(
                                1,
                                dto));
    }

    [Fact]
    public async Task
        DeleteAsync_WhenActiveBookingsExist_ShouldThrowConflictException()
    {
        var eventEntity =
            CreateExistingEvent();

        _eventRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        _eventRepositoryMock
            .Setup(x =>
                x.HasActiveBookingsAsync(1))
            .ReturnsAsync(true);

        await Assert
            .ThrowsAsync<
                ConflictException>(
                    () =>
                        _eventService
                            .DeleteAsync(1));

        _eventRepositoryMock.Verify(
            x =>
                x.Delete(
                    It.IsAny<EventEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task
        DeleteAsync_WhenNoActiveBookings_ShouldDeleteEvent()
    {
        var eventEntity =
            CreateExistingEvent();

        _eventRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        _eventRepositoryMock
            .Setup(x =>
                x.HasActiveBookingsAsync(1))
            .ReturnsAsync(false);

        _eventRepositoryMock
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        await _eventService
            .DeleteAsync(1);

        _eventRepositoryMock.Verify(
            x =>
                x.Delete(
                    eventEntity),
            Times.Once);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private void
        SetupVenueAndCategory(
            int venueCapacity = 500)
    {
        _venueRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(
                new Venue
                {
                    Id = 1,
                    Name =
                        "Main Hall",
                    Address =
                        "Vavuniya",
                    Capacity =
                        venueCapacity
                });

        _categoryRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(
                new EventCategory
                {
                    Id = 1,
                    Name =
                        "Conference"
                });
    }

    private void
        SetupUpdateDependencies(
            EventEntity eventEntity)
    {
        _eventRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(
                eventEntity);

        SetupVenueAndCategory();
    }

    private static EventCreateDto
        CreateValidCreateDto()
    {
        var start =
            DateTime.UtcNow
                .AddDays(30);

        return new EventCreateDto
        {
            Name =
                "Tech Conference",

            Description =
                "Technology conference",

            VenueId = 1,

            CategoryId = 1,

            StartDateTime =
                start,

            EndDateTime =
                start.AddHours(2),

            TicketPrice =
                2500m,

            ParkingFee =
                500m,

            Capacity =
                300
        };
    }

    private static EventUpdateDto
        CreateValidUpdateDto()
    {
        var start =
            DateTime.UtcNow
                .AddDays(30);

        return new EventUpdateDto
        {
            Name =
                "Tech Conference",

            Description =
                "Updated conference",

            VenueId = 1,

            CategoryId = 1,

            StartDateTime =
                start,

            EndDateTime =
                start.AddHours(2),

            TicketPrice =
                2500m,

            ParkingFee =
                500m,

            Capacity =
                300
        };
    }

    private static EventEntity
        CreateExistingEvent()
    {
        var start =
            DateTime.UtcNow
                .AddDays(30);

        return new EventEntity
        {
            Id = 1,

            Name =
                "Tech Conference",

            Description =
                "Technology conference",

            VenueId = 1,

            CategoryId = 1,

            Venue =
                new Venue
                {
                    Id = 1,
                    Name =
                        "Main Hall",
                    Address =
                        "Vavuniya",
                    Capacity =
                        500
                },

            Category =
                new EventCategory
                {
                    Id = 1,
                    Name =
                        "Conference"
                },

            StartDateTime =
                start,

            EndDateTime =
                start.AddHours(2),

            TicketPrice =
                2500m,

            ParkingFee =
                500m,

            Capacity =
                300
        };
    }
}