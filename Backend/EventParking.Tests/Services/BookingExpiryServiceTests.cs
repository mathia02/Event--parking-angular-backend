using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Moq;
using Xunit;

namespace EventParking.Tests.Services;

public class BookingExpiryServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<ISeatRepository> _seatRepositoryMock;
    private readonly Mock<IParkingRepository> _parkingRepositoryMock;

    private readonly BookingExpiryService _service;

    public BookingExpiryServiceTests()
    {
        _bookingRepositoryMock =
            new Mock<IBookingRepository>();

        _seatRepositoryMock =
            new Mock<ISeatRepository>();

        _parkingRepositoryMock =
            new Mock<IParkingRepository>();

        _service =
            new BookingExpiryService(
                _bookingRepositoryMock.Object,
                _seatRepositoryMock.Object,
                _parkingRepositoryMock.Object);
    }

    [Fact]
    public async Task ExpirePendingBookingsAsync_WhenNoExpiredBookings_ShouldReturnZero()
    {
        // Arrange
        _bookingRepositoryMock
            .Setup(x =>
                x.GetExpiredPendingBookingsAsync(
                    It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Booking>());

        SetupTransactionExecution();

        // Act
        var result =
            await _service
                .ExpirePendingBookingsAsync();

        // Assert
        Assert.Equal(0, result);

        _bookingRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);

        _seatRepositoryMock.Verify(
            x => x.Update(
                It.IsAny<Seat>()),
            Times.Never);

        _parkingRepositoryMock.Verify(
            x => x.Update(
                It.IsAny<ParkingSlot>()),
            Times.Never);
    }

    [Fact]
    public async Task ExpirePendingBookingsAsync_WhenPendingBookingExpired_ShouldExpireBookingAndReleaseResources()
    {
        // Arrange
        var seat =
            new Seat
            {
                Id = 3,
                EventId = 1,
                SeatNumber = "A3",
                RowLabel = "A",
                ColumnNumber = 3,
                SeatType = "Regular",
                Status = SeatStatus.Held
            };

        var booking =
            new Booking
            {
                Id = 2,
                BookingNumber = "BKG-TEST-EXPIRY-001",
                CustomerId = 3,
                EventId = 1,
                Status = BookingStatus.Pending,
                HoldExpiresAt =
                    DateTime.UtcNow.AddMinutes(-1),
                CreatedAt =
                    DateTime.UtcNow.AddMinutes(-16)
            };

        var bookingSeat =
            new BookingSeat
            {
                Id = 1,
                BookingId = booking.Id,
                SeatId = seat.Id,
                PriceAtBooking = 2500,
                IsActive = true,
                ReservedAt =
                    DateTime.UtcNow.AddMinutes(-16),
                Booking = booking,
                Seat = seat
            };

        var parkingSlot =
            new ParkingSlot
            {
                Id = 2,
                EventId = 1,
                SlotNumber = "A2",
                Zone = "Zone A",
                Fee = 500,
                Status = ParkingSlotStatus.Held
            };

        var parkingReservation =
            new ParkingReservation
            {
                Id = 1,
                BookingId = booking.Id,
                ParkingSlotId = parkingSlot.Id,
                ReservedFee = 500,
                IsActive = true,
                ReservedAt =
                    DateTime.UtcNow.AddMinutes(-16),
                Booking = booking,
                ParkingSlot = parkingSlot
            };

        booking.BookingSeats =
            new List<BookingSeat>
            {
                bookingSeat
            };

        booking.ParkingReservation =
            parkingReservation;

        _bookingRepositoryMock
            .Setup(x =>
                x.GetExpiredPendingBookingsAsync(
                    It.IsAny<DateTime>()))
            .ReturnsAsync(
                new List<Booking>
                {
                    booking
                });

        _bookingRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        SetupTransactionExecution();

        // Act
        var result =
            await _service
                .ExpirePendingBookingsAsync();

        // Assert
        Assert.Equal(1, result);

        Assert.Equal(
            BookingStatus.Expired,
            booking.Status);

        Assert.False(
            bookingSeat.IsActive);

        Assert.NotNull(
            bookingSeat.ReleasedAt);

        Assert.Equal(
            SeatStatus.Available,
            seat.Status);

        Assert.False(
            parkingReservation.IsActive);

        Assert.NotNull(
            parkingReservation.ReleasedAt);

        Assert.Equal(
            ParkingSlotStatus.Available,
            parkingSlot.Status);

        _seatRepositoryMock.Verify(
            x => x.Update(seat),
            Times.Once);

        _parkingRepositoryMock.Verify(
            x => x.Update(parkingSlot),
            Times.Once);

        _bookingRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task ExpirePendingBookingsAsync_WhenExpiredBookingHasNoParking_ShouldReleaseSeatOnly()
    {
        // Arrange
        var seat =
            new Seat
            {
                Id = 4,
                EventId = 1,
                SeatNumber = "A4",
                RowLabel = "A",
                ColumnNumber = 4,
                SeatType = "Regular",
                Status = SeatStatus.Held
            };

        var booking =
            new Booking
            {
                Id = 3,
                BookingNumber = "BKG-TEST-EXPIRY-002",
                CustomerId = 3,
                EventId = 1,
                Status = BookingStatus.Pending,
                HoldExpiresAt =
                    DateTime.UtcNow.AddMinutes(-1),
                CreatedAt =
                    DateTime.UtcNow.AddMinutes(-16)
            };

        var bookingSeat =
            new BookingSeat
            {
                Id = 2,
                BookingId = booking.Id,
                SeatId = seat.Id,
                PriceAtBooking = 2500,
                IsActive = true,
                ReservedAt =
                    DateTime.UtcNow.AddMinutes(-16),
                Booking = booking,
                Seat = seat
            };

        booking.BookingSeats =
            new List<BookingSeat>
            {
                bookingSeat
            };

        booking.ParkingReservation = null;

        _bookingRepositoryMock
            .Setup(x =>
                x.GetExpiredPendingBookingsAsync(
                    It.IsAny<DateTime>()))
            .ReturnsAsync(
                new List<Booking>
                {
                    booking
                });

        _bookingRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        SetupTransactionExecution();

        // Act
        var result =
            await _service
                .ExpirePendingBookingsAsync();

        // Assert
        Assert.Equal(1, result);

        Assert.Equal(
            BookingStatus.Expired,
            booking.Status);

        Assert.False(
            bookingSeat.IsActive);

        Assert.Equal(
            SeatStatus.Available,
            seat.Status);

        _seatRepositoryMock.Verify(
            x => x.Update(seat),
            Times.Once);

        _parkingRepositoryMock.Verify(
            x => x.Update(
                It.IsAny<ParkingSlot>()),
            Times.Never);

        _bookingRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task ExpirePendingBookingsAsync_WhenBookingIsNotPending_ShouldSkipBooking()
    {
        // Arrange
        var booking =
            new Booking
            {
                Id = 4,
                BookingNumber = "BKG-TEST-CONFIRMED",
                CustomerId = 3,
                EventId = 1,
                Status = BookingStatus.Confirmed,
                HoldExpiresAt =
                    DateTime.UtcNow.AddMinutes(-5),
                CreatedAt =
                    DateTime.UtcNow.AddMinutes(-20)
            };

        _bookingRepositoryMock
            .Setup(x =>
                x.GetExpiredPendingBookingsAsync(
                    It.IsAny<DateTime>()))
            .ReturnsAsync(
                new List<Booking>
                {
                    booking
                });

        SetupTransactionExecution();

        // Act
        var result =
            await _service
                .ExpirePendingBookingsAsync();

        // Assert
        Assert.Equal(0, result);

        Assert.Equal(
            BookingStatus.Confirmed,
            booking.Status);

        _bookingRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task ExpirePendingBookingsAsync_WhenHoldHasNotExpired_ShouldSkipBooking()
    {
        // Arrange
        var booking =
            new Booking
            {
                Id = 5,
                BookingNumber = "BKG-TEST-FUTURE-HOLD",
                CustomerId = 3,
                EventId = 1,
                Status = BookingStatus.Pending,
                HoldExpiresAt =
                    DateTime.UtcNow.AddMinutes(10),
                CreatedAt =
                    DateTime.UtcNow
            };

        _bookingRepositoryMock
            .Setup(x =>
                x.GetExpiredPendingBookingsAsync(
                    It.IsAny<DateTime>()))
            .ReturnsAsync(
                new List<Booking>
                {
                    booking
                });

        SetupTransactionExecution();

        // Act
        var result =
            await _service
                .ExpirePendingBookingsAsync();

        // Assert
        Assert.Equal(0, result);

        Assert.Equal(
            BookingStatus.Pending,
            booking.Status);

        _bookingRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task ExpirePendingBookingsAsync_ShouldExpireMultipleBookings()
    {
        // Arrange
        var booking1 =
            CreateExpiredPendingBooking(
                10);

        var booking2 =
            CreateExpiredPendingBooking(
                11);

        _bookingRepositoryMock
            .Setup(x =>
                x.GetExpiredPendingBookingsAsync(
                    It.IsAny<DateTime>()))
            .ReturnsAsync(
                new List<Booking>
                {
                    booking1,
                    booking2
                });

        _bookingRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(2);

        SetupTransactionExecution();

        // Act
        var result =
            await _service
                .ExpirePendingBookingsAsync();

        // Assert
        Assert.Equal(2, result);

        Assert.Equal(
            BookingStatus.Expired,
            booking1.Status);

        Assert.Equal(
            BookingStatus.Expired,
            booking2.Status);

        _bookingRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    private void SetupTransactionExecution()
    {
        _bookingRepositoryMock
            .Setup(x =>
                x.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>()))
            .Returns(
                (Func<Task> operation) =>
                    operation());
    }

    private static Booking
        CreateExpiredPendingBooking(int id)
    {
        return new Booking
        {
            Id = id,

            BookingNumber =
                $"BKG-EXP-{id}",

            CustomerId = 3,

            EventId = 1,

            Status =
                BookingStatus.Pending,

            HoldExpiresAt =
                DateTime.UtcNow.AddMinutes(-1),

            CreatedAt =
                DateTime.UtcNow.AddMinutes(-16),

            BookingSeats =
                new List<BookingSeat>()
        };
    }
}