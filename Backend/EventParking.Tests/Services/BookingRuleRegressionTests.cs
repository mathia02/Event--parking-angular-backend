using EventParking.Business.Exceptions;
using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Booking;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Moq;
using Xunit;

using EventEntity =
    EventParking.Models.Entities.Event;

namespace EventParking.Tests.Services;

public class BookingRuleRegressionTests
{
    private readonly Mock<IBookingRepository>
        _bookingRepositoryMock;

    private readonly Mock<ICustomerRepository>
        _customerRepositoryMock;

    private readonly Mock<IEventRepository>
        _eventRepositoryMock;

    private readonly Mock<ISeatRepository>
        _seatRepositoryMock;

    private readonly Mock<IParkingRepository>
        _parkingRepositoryMock;

    private readonly BookingService
        _bookingService;

    public BookingRuleRegressionTests()
    {
        _bookingRepositoryMock =
            new Mock<IBookingRepository>();

        _customerRepositoryMock =
            new Mock<ICustomerRepository>();

        _eventRepositoryMock =
            new Mock<IEventRepository>();

        _seatRepositoryMock =
            new Mock<ISeatRepository>();

        _parkingRepositoryMock =
            new Mock<IParkingRepository>();

        _bookingRepositoryMock
            .Setup(x =>
                x.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(
                async operation =>
                    await operation());

        _bookingService =
            new BookingService(
                _bookingRepositoryMock.Object,
                _customerRepositoryMock.Object,
                _eventRepositoryMock.Object,
                _seatRepositoryMock.Object,
                _parkingRepositoryMock.Object);
    }

    // =========================================================
    // STEP 05
    // FREE BOOKING AUTO CONFIRM
    // =========================================================

    [Fact]
    public async Task
        CreateAsync_WhenBookingTotalIsZero_ShouldConfirmImmediately()
    {
        var customer =
            CreateCustomer();

        var eventEntity =
            CreateEvent(
                ticketPrice: 0m);

        var seat =
            CreateSeat(
                SeatStatus.Available);

        _customerRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(3))
            .ReturnsAsync(customer);

        _eventRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        _seatRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(10))
            .ReturnsAsync(seat);

        _bookingRepositoryMock
            .Setup(x =>
                x.BookingNumberExistsAsync(
                    It.IsAny<string>()))
            .ReturnsAsync(false);

        Booking? capturedBooking =
            null;

        List<BookingSeat>
            capturedSeats =
                new();

        _bookingRepositoryMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<Booking>()))
            .Callback<Booking>(
                booking =>
                {
                    booking.Id = 1;

                    capturedBooking =
                        booking;
                })
            .Returns(Task.CompletedTask);

        _bookingRepositoryMock
            .Setup(x =>
                x.AddBookingSeatsAsync(
                    It.IsAny<
                        IEnumerable<
                            BookingSeat>>()))
            .Callback<
                IEnumerable<BookingSeat>>(
                    bookingSeats =>
                    {
                        capturedSeats =
                            bookingSeats
                                .ToList();
                    })
            .Returns(Task.CompletedTask);

        _bookingRepositoryMock
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        _bookingRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(() =>
            {
                if (capturedBooking ==
                    null)
                {
                    return null;
                }

                capturedBooking.Customer =
                    customer;

                capturedBooking.Event =
                    eventEntity;

                capturedBooking.BookingSeats =
                    capturedSeats;

                foreach (
                    var bookingSeat
                    in capturedSeats)
                {
                    bookingSeat.Booking =
                        capturedBooking;

                    bookingSeat.Seat =
                        seat;
                }

                return capturedBooking;
            });

        var dto =
            new BookingCreateDto
            {
                EventId = 1,

                SeatIds =
                    new List<int>
                    {
                        10
                    },

                ParkingSlotId =
                    null
            };

        var result =
            await _bookingService
                .CreateAsync(
                    3,
                    dto);

        Assert.Equal(
            BookingStatus.Confirmed,
            result.Status);

        Assert.Null(
            result.HoldExpiresAt);

        Assert.NotNull(
            result.ConfirmedAt);

        Assert.Equal(
            0m,
            result.TotalAmount);

        Assert.Equal(
            SeatStatus.Booked,
            seat.Status);

        Assert.NotNull(
            capturedBooking);

        Assert.Equal(
            BookingStatus.Confirmed,
            capturedBooking!.Status);
    }

    // =========================================================
    // STEP 06
    // PAID CONFIRMED BOOKING CANNOT CANCEL
    // =========================================================

    [Fact]
    public async Task
        CancelAsync_WhenPaidConfirmedBooking_ShouldThrowConflictException()
    {
        var booking =
            CreateConfirmedBooking();

        booking.Payment =
            new Payment
            {
                BookingId =
                    booking.Id,

                Amount =
                    2500m,

                Status =
                    PaymentStatus.Completed
            };

        _bookingRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(booking);

        var exception =
            await Assert
                .ThrowsAsync<
                    ConflictException>(
                    () =>
                        _bookingService
                            .CancelAsync(
                                1,
                                3,
                                false));

        Assert.Contains(
            "refund",
            exception.Message,
            StringComparison
                .OrdinalIgnoreCase);

        _bookingRepositoryMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Never);
    }

    // =========================================================
    // FREE CONFIRMED BOOKING CAN CANCEL
    // =========================================================

    [Fact]
    public async Task
        CancelAsync_WhenFreeConfirmedBooking_ShouldCancelAndReleaseSeat()
    {
        var booking =
            CreateConfirmedBooking();

        booking.Payment =
            null;

        var seat =
            CreateSeat(
                SeatStatus.Booked);

        var bookingSeat =
            new BookingSeat
            {
                Id = 1,

                BookingId = 1,

                SeatId =
                    seat.Id,

                PriceAtBooking =
                    0m,

                IsActive =
                    true,

                ReservedAt =
                    DateTime.UtcNow,

                Booking =
                    booking,

                Seat =
                    seat
            };

        booking.BookingSeats =
            new List<BookingSeat>
            {
                bookingSeat
            };

        _bookingRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(booking);

        _bookingRepositoryMock
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result =
            await _bookingService
                .CancelAsync(
                    1,
                    3,
                    false);

        Assert.Equal(
            BookingStatus.Cancelled,
            result.Status);

        Assert.Equal(
            BookingStatus.Cancelled,
            booking.Status);

        Assert.NotNull(
            booking.CancelledAt);

        Assert.False(
            bookingSeat.IsActive);

        Assert.NotNull(
            bookingSeat.ReleasedAt);

        Assert.Equal(
            SeatStatus.Available,
            seat.Status);

        _seatRepositoryMock.Verify(
            x =>
                x.Update(seat),
            Times.Once);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static Customer
        CreateCustomer()
    {
        return new Customer
        {
            Id = 3,

            FullName =
                "Test Customer",

            Email =
                "customer1@test.com",

            Phone =
                "0771234567",

            PasswordHash =
                "test",

            Role =
                "Customer",

            Status =
                CustomerStatus.Active,

            EmailVerified =
                true,

            CreatedAt =
                DateTime.UtcNow
        };
    }

    private static EventEntity
        CreateEvent(
            decimal ticketPrice)
    {
        var start =
            DateTime.UtcNow
                .AddDays(30);

        return new EventEntity
        {
            Id = 1,

            Name =
                "Test Event",

            VenueId = 1,

            CategoryId = 1,

            StartDateTime =
                start,

            EndDateTime =
                start.AddHours(2),

            TicketPrice =
                ticketPrice,

            ParkingFee =
                0m,

            Capacity =
                100
        };
    }

    private static Seat
        CreateSeat(
            SeatStatus status)
    {
        return new Seat
        {
            Id = 10,

            EventId = 1,

            SeatNumber =
                "A1",

            RowLabel =
                "A",

            ColumnNumber =
                1,

            SeatType =
                "Regular",

            PriceOverride =
                null,

            Status =
                status
        };
    }

    private static Booking
        CreateConfirmedBooking()
    {
        return new Booking
        {
            Id = 1,

            BookingNumber =
                "BKG-TEST-001",

            CustomerId = 3,

            EventId = 1,

            Status =
                BookingStatus.Confirmed,

            HoldExpiresAt =
                null,

            CreatedAt =
                DateTime.UtcNow,

            ConfirmedAt =
                DateTime.UtcNow,

            Customer =
                CreateCustomer(),

            Event =
                CreateEvent(
                    2500m),

            BookingSeats =
                new List<BookingSeat>()
        };
    }
}