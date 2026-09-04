using EventParking.Business.Exceptions;
using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Seat;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Moq;
using Xunit;

using EventEntity = EventParking.Models.Entities.Event;

namespace EventParking.Tests.Services;

public class SeatServiceTests
{
    private readonly Mock<ISeatRepository> _seatRepositoryMock;
    private readonly Mock<IEventRepository> _eventRepositoryMock;

    private readonly SeatService _seatService;

    public SeatServiceTests()
    {
        _seatRepositoryMock =
            new Mock<ISeatRepository>();

        _eventRepositoryMock =
            new Mock<IEventRepository>();

        _seatService =
            new SeatService(
                _seatRepositoryMock.Object,
                _eventRepositoryMock.Object);
    }

    [Fact]
    public async Task GetByEventIdAsync_ShouldReturnSeats()
    {
        // Arrange
        var eventEntity = CreateEvent();

        var seats = new List<Seat>
        {
            new Seat
            {
                Id = 1,
                EventId = 1,
                SeatNumber = "A1",
                RowLabel = "A",
                ColumnNumber = 1,
                SeatType = "Regular",
                Status = SeatStatus.Available
            },

            new Seat
            {
                Id = 2,
                EventId = 1,
                SeatNumber = "A2",
                RowLabel = "A",
                ColumnNumber = 2,
                SeatType = "Regular",
                Status = SeatStatus.Available
            }
        };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        _seatRepositoryMock
            .Setup(x => x.GetByEventIdAsync(1))
            .ReturnsAsync(seats);

        // Act
        var result =
            await _seatService.GetByEventIdAsync(
                1,
                false);

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(
            "A1",
            result[0].SeatNumber);

        Assert.Equal(
            "A2",
            result[1].SeatNumber);
    }

    [Fact]
    public async Task GetByIdAsync_WhenSeatDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        _seatRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Seat?)null);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () =>
                _seatService.GetByIdAsync(999));
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateSeat()
    {
        // Arrange
        var eventEntity = CreateEvent();

        eventEntity.Capacity = 10;

        var dto = new SeatCreateDto
        {
            SeatNumber = "A1",
            RowLabel = "A",
            ColumnNumber = 1,
            SeatType = "Regular",
            PriceOverride = null
        };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        _seatRepositoryMock
            .Setup(x =>
                x.GetSeatCountByEventAsync(1))
            .ReturnsAsync(2);

        _seatRepositoryMock
            .Setup(x =>
                x.SeatNumberExistsAsync(
                    1,
                    "A1",
                    null))
            .ReturnsAsync(false);

        _seatRepositoryMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<Seat>()))
            .Callback<Seat>(
                seat =>
                    seat.Id = 1)
            .Returns(Task.CompletedTask);

        _seatRepositoryMock
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result =
            await _seatService.CreateAsync(
                1,
                dto);

        // Assert
        Assert.Equal(
            "A1",
            result.SeatNumber);

        Assert.Equal(
            "A",
            result.RowLabel);

        Assert.Equal(
            SeatStatus.Available,
            result.Status);

        _seatRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<Seat>()),
            Times.Once);

        _seatRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenEventDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var dto = new SeatCreateDto
        {
            SeatNumber = "A1",
            RowLabel = "A",
            ColumnNumber = 1
        };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((EventEntity?)null);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () =>
                _seatService.CreateAsync(
                    999,
                    dto));

        _seatRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<Seat>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenEventCapacityIsFull_ShouldThrowValidationException()
    {
        // Arrange
        var eventEntity = CreateEvent();

        eventEntity.Capacity = 300;

        var dto = new SeatCreateDto
        {
            SeatNumber = "P1",
            RowLabel = "P",
            ColumnNumber = 1,
            SeatType = "Regular"
        };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        _seatRepositoryMock
            .Setup(x =>
                x.GetSeatCountByEventAsync(1))
            .ReturnsAsync(300);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            () =>
                _seatService.CreateAsync(
                    1,
                    dto));

        _seatRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<Seat>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenSeatNumberAlreadyExists_ShouldThrowConflictException()
    {
        // Arrange
        var eventEntity = CreateEvent();

        eventEntity.Capacity = 300;

        var dto = new SeatCreateDto
        {
            SeatNumber = "A1",
            RowLabel = "A",
            ColumnNumber = 1,
            SeatType = "Regular"
        };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        _seatRepositoryMock
            .Setup(x =>
                x.GetSeatCountByEventAsync(1))
            .ReturnsAsync(100);

        _seatRepositoryMock
            .Setup(x =>
                x.SeatNumberExistsAsync(
                    1,
                    "A1",
                    null))
            .ReturnsAsync(true);

        // Act + Assert
        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _seatService.CreateAsync(
                    1,
                    dto));

        _seatRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<Seat>()),
            Times.Never);
    }

    [Fact]
    public async Task GenerateSeatMapAsync_WithValidData_ShouldGenerateCorrectSeatCount()
    {
        // Arrange
        var eventEntity = CreateEvent();

        eventEntity.Capacity = 4;

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        _seatRepositoryMock
            .Setup(x =>
                x.GetSeatCountByEventAsync(1))
            .ReturnsAsync(0);

        List<Seat>? generatedSeats = null;

        _seatRepositoryMock
            .Setup(x =>
                x.AddRangeAsync(
                    It.IsAny<IEnumerable<Seat>>()))
            .Callback<IEnumerable<Seat>>(
                seats =>
                    generatedSeats =
                        seats.ToList())
            .Returns(Task.CompletedTask);

        _seatRepositoryMock
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result =
            await _seatService
                .GenerateSeatMapAsync(
                    1,
                    2,
                    2,
                    "Regular",
                    null);

        // Assert
        Assert.Equal(4, result.Count);

        Assert.NotNull(generatedSeats);

        Assert.Contains(
            generatedSeats!,
            x => x.SeatNumber == "A1");

        Assert.Contains(
            generatedSeats!,
            x => x.SeatNumber == "A2");

        Assert.Contains(
            generatedSeats!,
            x => x.SeatNumber == "B1");

        Assert.Contains(
            generatedSeats!,
            x => x.SeatNumber == "B2");

        _seatRepositoryMock.Verify(
            x =>
                x.AddRangeAsync(
                    It.IsAny<IEnumerable<Seat>>()),
            Times.Once);

        _seatRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GenerateSeatMapAsync_WhenSeatMapAlreadyExists_ShouldThrowConflictException()
    {
        // Arrange
        var eventEntity = CreateEvent();

        eventEntity.Capacity = 300;

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        _seatRepositoryMock
            .Setup(x =>
                x.GetSeatCountByEventAsync(1))
            .ReturnsAsync(300);

        // Act + Assert
        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _seatService.GenerateSeatMapAsync(
                    1,
                    15,
                    20,
                    "Regular",
                    null));

        _seatRepositoryMock.Verify(
            x =>
                x.AddRangeAsync(
                    It.IsAny<IEnumerable<Seat>>()),
            Times.Never);
    }

    [Fact]
    public async Task GenerateSeatMapAsync_WhenSeatCountDoesNotMatchEventCapacity_ShouldThrowValidationException()
    {
        // Arrange
        var eventEntity = CreateEvent();

        eventEntity.Capacity = 300;

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        _seatRepositoryMock
            .Setup(x =>
                x.GetSeatCountByEventAsync(1))
            .ReturnsAsync(0);

        // 10 x 20 = 200
        // Event capacity = 300
        // Therefore reject.
        await Assert.ThrowsAsync<ValidationException>(
            () =>
                _seatService.GenerateSeatMapAsync(
                    1,
                    10,
                    20,
                    "Regular",
                    null));

        _seatRepositoryMock.Verify(
            x =>
                x.AddRangeAsync(
                    It.IsAny<IEnumerable<Seat>>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenSeatHasActiveBooking_ShouldThrowConflictException()
    {
        // Arrange
        var seat = CreateSeat();

        var dto = new SeatUpdateDto
        {
            SeatNumber = "A1",
            RowLabel = "A",
            ColumnNumber = 1,
            SeatType = "VIP",
            PriceOverride = 3500
        };

        _seatRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(seat);

        _seatRepositoryMock
            .Setup(x =>
                x.HasActiveBookingAsync(1))
            .ReturnsAsync(true);

        // Act + Assert
        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _seatService.UpdateAsync(
                    1,
                    dto));

        _seatRepositoryMock.Verify(
            x =>
                x.Update(
                    It.IsAny<Seat>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenSeatNumberAlreadyExists_ShouldThrowConflictException()
    {
        // Arrange
        var seat = CreateSeat();

        var dto = new SeatUpdateDto
        {
            SeatNumber = "A2",
            RowLabel = "A",
            ColumnNumber = 2,
            SeatType = "Regular",
            PriceOverride = null
        };

        _seatRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(seat);

        _seatRepositoryMock
            .Setup(x =>
                x.HasActiveBookingAsync(1))
            .ReturnsAsync(false);

        _seatRepositoryMock
            .Setup(x =>
                x.SeatNumberExistsAsync(
                    1,
                    "A2",
                    1))
            .ReturnsAsync(true);

        // Act + Assert
        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _seatService.UpdateAsync(
                    1,
                    dto));

        _seatRepositoryMock.Verify(
            x =>
                x.Update(
                    It.IsAny<Seat>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldUpdateSeat()
    {
        // Arrange
        var seat = CreateSeat();

        var dto = new SeatUpdateDto
        {
            SeatNumber = "A1",
            RowLabel = "A",
            ColumnNumber = 1,
            SeatType = "VIP",
            PriceOverride = 3500
        };

        _seatRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(seat);

        _seatRepositoryMock
            .Setup(x =>
                x.HasActiveBookingAsync(1))
            .ReturnsAsync(false);

        _seatRepositoryMock
            .Setup(x =>
                x.SeatNumberExistsAsync(
                    1,
                    "A1",
                    1))
            .ReturnsAsync(false);

        _seatRepositoryMock
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result =
            await _seatService.UpdateAsync(
                1,
                dto);

        // Assert
        Assert.Equal(
            "VIP",
            result.SeatType);

        Assert.Equal(
            3500,
            result.PriceOverride);

        Assert.Equal(
            "A1",
            result.SeatNumber);

        _seatRepositoryMock.Verify(
            x => x.Update(seat),
            Times.Once);

        _seatRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenSeatHasActiveBooking_ShouldThrowConflictException()
    {
        // Arrange
        var seat = CreateSeat();

        _seatRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(seat);

        _seatRepositoryMock
            .Setup(x =>
                x.HasActiveBookingAsync(1))
            .ReturnsAsync(true);

        // Act + Assert
        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _seatService.DeleteAsync(1));

        _seatRepositoryMock.Verify(
            x =>
                x.Delete(
                    It.IsAny<Seat>()),
            Times.Never);

        _seatRepositoryMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenSeatHasNoActiveBooking_ShouldDeleteSeat()
    {
        // Arrange
        var seat = CreateSeat();

        _seatRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(seat);

        _seatRepositoryMock
            .Setup(x =>
                x.HasActiveBookingAsync(1))
            .ReturnsAsync(false);

        _seatRepositoryMock
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _seatService.DeleteAsync(1);

        // Assert
        _seatRepositoryMock.Verify(
            x => x.Delete(seat),
            Times.Once);

        _seatRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    private static EventEntity CreateEvent()
    {
        return new EventEntity
        {
            Id = 1,
            Name = "Tech Conference 2026",
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

    private static Seat CreateSeat()
    {
        return new Seat
        {
            Id = 1,
            EventId = 1,
            SeatNumber = "A1",
            RowLabel = "A",
            ColumnNumber = 1,
            SeatType = "Regular",
            PriceOverride = null,
            Status = SeatStatus.Available
        };
    }
}