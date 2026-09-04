using EventParking.Business.Exceptions;
using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Booking;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Moq;
using Xunit;

using EventEntity = EventParking.Models.Entities.Event;

namespace EventParking.Tests.Services;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<ISeatRepository> _seatRepositoryMock;
    private readonly Mock<IParkingRepository> _parkingRepositoryMock;

    private readonly BookingService _bookingService;

    public BookingServiceTests()
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

        _bookingService =
            new BookingService(
                _bookingRepositoryMock.Object,
                _customerRepositoryMock.Object,
                _eventRepositoryMock.Object,
                _seatRepositoryMock.Object,
                _parkingRepositoryMock.Object);
    }

    // ---------------------------------------------------------
    // GET ALL
    // ---------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_ShouldReturnBookings()
    {
        var bookings = new List<Booking>
        {
            CreateBooking()
        };

        _bookingRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(bookings);

        var result =
            await _bookingService.GetAllAsync();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Tech Conference 2026", result[0].EventName);
    }

    // ---------------------------------------------------------
    // GET MY BOOKINGS
    // ---------------------------------------------------------

    [Fact]
    public async Task GetMyBookingsAsync_ShouldReturnCustomerBookings()
    {
        var bookings = new List<Booking>
        {
            CreateBooking()
        };

        _bookingRepositoryMock
            .Setup(x => x.GetByCustomerIdAsync(3))
            .ReturnsAsync(bookings);

        var result =
            await _bookingService
                .GetMyBookingsAsync(3);

        Assert.Single(result);
        Assert.Equal(3, result[0].CustomerId);

        _bookingRepositoryMock.Verify(
            x => x.GetByCustomerIdAsync(3),
            Times.Once);
    }

    // ---------------------------------------------------------
    // GET BY ID
    // ---------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_WhenBookingDoesNotExist_ShouldThrowNotFoundException()
    {
        _bookingRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Booking?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () =>
                _bookingService.GetByIdAsync(
                    999,
                    3,
                    false));
    }

    [Fact]
    public async Task GetByIdAsync_WhenAnotherCustomerRequestsBooking_ShouldThrowUnauthorizedAccessException()
    {
        var booking = CreateBooking();

        _bookingRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(booking);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () =>
                _bookingService.GetByIdAsync(
                    1,
                    999,
                    false));
    }

    [Fact]
    public async Task GetByIdAsync_WhenAdministratorRequestsBooking_ShouldReturnBooking()
    {
        var booking = CreateBooking();

        _bookingRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(booking);

        var result =
            await _bookingService.GetByIdAsync(
                1,
                999,
                true);

        Assert.Equal(1, result.Id);
        Assert.Equal(3, result.CustomerId);
    }

    // ---------------------------------------------------------
    // CREATE VALIDATION
    // ---------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WhenSeatListIsEmpty_ShouldThrowValidationException()
    {
        var dto = new BookingCreateDto
        {
            EventId = 1,
            SeatIds = new List<int>(),
            ParkingSlotId = null
        };

        await Assert.ThrowsAsync<ValidationException>(
            () =>
                _bookingService.CreateAsync(
                    3,
                    dto));
    }

    [Fact]
    public async Task CreateAsync_WhenSameSeatIsSelectedTwice_ShouldThrowValidationException()
    {
        var dto = new BookingCreateDto
        {
            EventId = 1,
            SeatIds = new List<int>
            {
                1,
                1
            }
        };

        await Assert.ThrowsAsync<ValidationException>(
            () =>
                _bookingService.CreateAsync(
                    3,
                    dto));
    }

    [Fact]
    public async Task CreateAsync_WhenCustomerDoesNotExist_ShouldThrowNotFoundException()
    {
        var dto = CreateBookingCreateDto();

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () =>
                _bookingService.CreateAsync(
                    999,
                    dto));
    }

    [Fact]
    public async Task CreateAsync_WhenCustomerEmailIsNotVerified_ShouldThrowValidationException()
    {
        var customer = CreateCustomer();

        customer.EmailVerified = false;

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(3))
            .ReturnsAsync(customer);

        await Assert.ThrowsAsync<ValidationException>(
            () =>
                _bookingService.CreateAsync(
                    3,
                    CreateBookingCreateDto()));
    }

    [Fact]
    public async Task CreateAsync_WhenCustomerIsDeactivated_ShouldThrowValidationException()
    {
        var customer = CreateCustomer();

        customer.Status =
            CustomerStatus.Deactivated;

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(3))
            .ReturnsAsync(customer);

        await Assert.ThrowsAsync<ValidationException>(
            () =>
                _bookingService.CreateAsync(
                    3,
                    CreateBookingCreateDto()));
    }

    [Fact]
    public async Task CreateAsync_WhenEventDoesNotExist_ShouldThrowNotFoundException()
    {
        var customer = CreateCustomer();

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(3))
            .ReturnsAsync(customer);

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((EventEntity?)null);

        var dto = new BookingCreateDto
        {
            EventId = 999,
            SeatIds = new List<int>
            {
                1
            }
        };

        await Assert.ThrowsAsync<NotFoundException>(
            () =>
                _bookingService.CreateAsync(
                    3,
                    dto));
    }

    // ---------------------------------------------------------
    // SEAT VALIDATION
    // ---------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WhenSeatDoesNotExist_ShouldThrowNotFoundException()
    {
        SetupValidCustomerAndEvent();

        _seatRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Seat?)null);

        SetupTransactionExecution();

        var dto = new BookingCreateDto
        {
            EventId = 1,
            SeatIds = new List<int>
            {
                999
            }
        };

        await Assert.ThrowsAsync<NotFoundException>(
            () =>
                _bookingService.CreateAsync(
                    3,
                    dto));
    }

    [Fact]
    public async Task CreateAsync_WhenSeatBelongsToAnotherEvent_ShouldThrowValidationException()
    {
        SetupValidCustomerAndEvent();

        var seat = CreateSeat(
            1,
            "A1",
            SeatStatus.Available);

        seat.EventId = 2;

        _seatRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(seat);

        SetupTransactionExecution();

        await Assert.ThrowsAsync<ValidationException>(
            () =>
                _bookingService.CreateAsync(
                    3,
                    CreateBookingCreateDto()));
    }

    [Fact]
    public async Task CreateAsync_WhenSeatIsHeld_ShouldThrowConflictException()
    {
        SetupValidCustomerAndEvent();

        var seat =
            CreateSeat(
                1,
                "A1",
                SeatStatus.Held);

        _seatRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(seat);

        SetupTransactionExecution();

        var dto = new BookingCreateDto
        {
            EventId = 1,
            SeatIds = new List<int>
            {
                1
            }
        };

        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _bookingService.CreateAsync(
                    3,
                    dto));
    }

    // ---------------------------------------------------------
    // PARKING VALIDATION
    // ---------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WhenParkingSlotDoesNotExist_ShouldThrowNotFoundException()
    {
        SetupValidCustomerAndEvent();

        var seat =
            CreateSeat(
                1,
                "A1",
                SeatStatus.Available);

        _seatRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(seat);

        _parkingRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((ParkingSlot?)null);

        SetupTransactionExecution();

        var dto = new BookingCreateDto
        {
            EventId = 1,
            SeatIds = new List<int>
            {
                1
            },
            ParkingSlotId = 999
        };

        await Assert.ThrowsAsync<NotFoundException>(
            () =>
                _bookingService.CreateAsync(
                    3,
                    dto));
    }

    [Fact]
    public async Task CreateAsync_WhenParkingSlotIsHeld_ShouldThrowConflictException()
    {
        SetupValidCustomerAndEvent();

        var seat =
            CreateSeat(
                3,
                "A3",
                SeatStatus.Available);

        var parking =
            CreateParkingSlot();

        parking.Status =
            ParkingSlotStatus.Held;

        _seatRepositoryMock
            .Setup(x => x.GetByIdAsync(3))
            .ReturnsAsync(seat);

        _parkingRepositoryMock
            .Setup(x => x.GetByIdAsync(2))
            .ReturnsAsync(parking);

        SetupTransactionExecution();

        var dto = new BookingCreateDto
        {
            EventId = 1,
            SeatIds = new List<int>
            {
                3
            },
            ParkingSlotId = 2
        };

        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _bookingService.CreateAsync(
                    3,
                    dto));

        Assert.Equal(
            SeatStatus.Available,
            seat.Status);
    }

    // ---------------------------------------------------------
    // VALID BOOKING CREATE
    // ---------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WithSeatsAndParking_ShouldCreatePendingBookingAndHoldResources()
    {
        var customer = CreateCustomer();
        var eventEntity = CreateEvent();

        var seat1 =
            CreateSeat(
                1,
                "A1",
                SeatStatus.Available);

        seat1.SeatType = "VIP";
        seat1.PriceOverride = 3500;

        var seat2 =
            CreateSeat(
                2,
                "A2",
                SeatStatus.Available);

        seat2.PriceOverride = null;

        var parkingSlot =
            CreateParkingSlot();

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(3))
            .ReturnsAsync(customer);

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        _seatRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(seat1);

        _seatRepositoryMock
            .Setup(x => x.GetByIdAsync(2))
            .ReturnsAsync(seat2);

        _parkingRepositoryMock
            .Setup(x => x.GetByIdAsync(2))
            .ReturnsAsync(parkingSlot);

        _bookingRepositoryMock
            .Setup(x =>
                x.BookingNumberExistsAsync(
                    It.IsAny<string>()))
            .ReturnsAsync(false);

        Booking? capturedBooking = null;
        List<BookingSeat> capturedBookingSeats = new();
        ParkingReservation? capturedParking = null;

        _bookingRepositoryMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<Booking>()))
            .Callback<Booking>(
                booking =>
                {
                    booking.Id = 1;
                    capturedBooking = booking;
                })
            .Returns(Task.CompletedTask);

        _bookingRepositoryMock
            .Setup(x =>
                x.AddBookingSeatsAsync(
                    It.IsAny<IEnumerable<BookingSeat>>()))
            .Callback<IEnumerable<BookingSeat>>(
                seats =>
                {
                    capturedBookingSeats =
                        seats.ToList();
                })
            .Returns(Task.CompletedTask);

        _bookingRepositoryMock
            .Setup(x =>
                x.AddParkingReservationAsync(
                    It.IsAny<ParkingReservation>()))
            .Callback<ParkingReservation>(
                reservation =>
                {
                    capturedParking =
                        reservation;
                })
            .Returns(Task.CompletedTask);

        _bookingRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        SetupTransactionExecution();

        _bookingRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(() =>
            {
                if (capturedBooking == null)
                {
                    return null;
                }

                capturedBooking.Customer =
                    customer;

                capturedBooking.Event =
                    eventEntity;

                capturedBooking.BookingSeats =
                    capturedBookingSeats;

                foreach (var bookingSeat in capturedBookingSeats)
                {
                    bookingSeat.Seat =
                        bookingSeat.SeatId == 1
                            ? seat1
                            : seat2;

                    bookingSeat.Booking =
                        capturedBooking;
                }

                if (capturedParking != null)
                {
                    capturedParking.ParkingSlot =
                        parkingSlot;

                    capturedParking.Booking =
                        capturedBooking;

                    capturedBooking.ParkingReservation =
                        capturedParking;
                }

                return capturedBooking;
            });

        var dto = new BookingCreateDto
        {
            EventId = 1,
            SeatIds = new List<int>
            {
                1,
                2
            },
            ParkingSlotId = 2
        };

        var result =
            await _bookingService.CreateAsync(
                3,
                dto);

        Assert.Equal(1, result.Id);

        Assert.Equal(
            BookingStatus.Pending,
            result.Status);

        Assert.Equal(2, result.Seats.Count);

        Assert.NotNull(result.Parking);

        Assert.Equal(
            6500m,
            result.TotalAmount);

        Assert.Equal(
            SeatStatus.Held,
            seat1.Status);

        Assert.Equal(
            SeatStatus.Held,
            seat2.Status);

        Assert.Equal(
            ParkingSlotStatus.Held,
            parkingSlot.Status);

        Assert.NotNull(capturedBooking);

        Assert.True(
            capturedBooking!.HoldExpiresAt.HasValue);

        var holdDifference =
            capturedBooking.HoldExpiresAt!.Value -
            capturedBooking.CreatedAt;

        Assert.True(
            holdDifference.TotalMinutes >= 14.9 &&
            holdDifference.TotalMinutes <= 15.1);

        _seatRepositoryMock.Verify(
            x => x.Update(seat1),
            Times.Once);

        _seatRepositoryMock.Verify(
            x => x.Update(seat2),
            Times.Once);

        _parkingRepositoryMock.Verify(
            x => x.Update(parkingSlot),
            Times.Once);
    }

    // ---------------------------------------------------------
    // CANCEL VALIDATION
    // ---------------------------------------------------------

    [Fact]
    public async Task CancelAsync_WhenBookingDoesNotExist_ShouldThrowNotFoundException()
    {
        _bookingRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Booking?)null);

        SetupTransactionExecution();

        await Assert.ThrowsAsync<NotFoundException>(
            () =>
                _bookingService.CancelAsync(
                    999,
                    3,
                    false));
    }

    [Fact]
    public async Task CancelAsync_WhenAnotherCustomerCancels_ShouldThrowUnauthorizedAccessException()
    {
        var booking =
            CreateBooking();

        _bookingRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(booking);

        SetupTransactionExecution();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () =>
                _bookingService.CancelAsync(
                    1,
                    999,
                    false));
    }

    [Fact]
    public async Task CancelAsync_WhenBookingAlreadyCancelled_ShouldThrowConflictException()
    {
        var booking =
            CreateBooking();

        booking.Status =
            BookingStatus.Cancelled;

        _bookingRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(booking);

        SetupTransactionExecution();

        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _bookingService.CancelAsync(
                    1,
                    3,
                    false));
    }

    [Fact]
    public async Task CancelAsync_WhenBookingIsExpired_ShouldThrowConflictException()
    {
        var booking =
            CreateBooking();

        booking.Status =
            BookingStatus.Expired;

        _bookingRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(booking);

        SetupTransactionExecution();

        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _bookingService.CancelAsync(
                    1,
                    3,
                    false));
    }

    // ---------------------------------------------------------
    // VALID CANCEL
    // ---------------------------------------------------------

    [Fact]
    public async Task CancelAsync_WithPendingBooking_ShouldCancelAndReleaseSeatsAndParking()
    {
        var booking =
            CreateBooking();

        var seat1 =
            CreateSeat(
                1,
                "A1",
                SeatStatus.Held);

        var seat2 =
            CreateSeat(
                2,
                "A2",
                SeatStatus.Held);

        var bookingSeat1 =
            new BookingSeat
            {
                Id = 1,
                BookingId = 1,
                SeatId = 1,
                PriceAtBooking = 3500,
                IsActive = true,
                ReservedAt = DateTime.UtcNow,
                Booking = booking,
                Seat = seat1
            };

        var bookingSeat2 =
            new BookingSeat
            {
                Id = 2,
                BookingId = 1,
                SeatId = 2,
                PriceAtBooking = 2500,
                IsActive = true,
                ReservedAt = DateTime.UtcNow,
                Booking = booking,
                Seat = seat2
            };

        var parkingSlot =
            CreateParkingSlot();

        parkingSlot.Status =
            ParkingSlotStatus.Held;

        var parkingReservation =
            new ParkingReservation
            {
                Id = 1,
                BookingId = 1,
                ParkingSlotId = 2,
                ReservedFee = 500,
                IsActive = true,
                ReservedAt = DateTime.UtcNow,
                Booking = booking,
                ParkingSlot = parkingSlot
            };

        booking.BookingSeats =
            new List<BookingSeat>
            {
                bookingSeat1,
                bookingSeat2
            };

        booking.ParkingReservation =
            parkingReservation;

        _bookingRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(booking);

        _bookingRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        SetupTransactionExecution();

        var result =
            await _bookingService.CancelAsync(
                1,
                3,
                false);

        Assert.Equal(
            BookingStatus.Cancelled,
            booking.Status);

        Assert.NotNull(
            booking.CancelledAt);

        Assert.False(
            bookingSeat1.IsActive);

        Assert.False(
            bookingSeat2.IsActive);

        Assert.NotNull(
            bookingSeat1.ReleasedAt);

        Assert.NotNull(
            bookingSeat2.ReleasedAt);

        Assert.Equal(
            SeatStatus.Available,
            seat1.Status);

        Assert.Equal(
            SeatStatus.Available,
            seat2.Status);

        Assert.False(
            parkingReservation.IsActive);

        Assert.NotNull(
            parkingReservation.ReleasedAt);

        Assert.Equal(
            ParkingSlotStatus.Available,
            parkingSlot.Status);

        Assert.Equal(
            BookingStatus.Cancelled,
            result.Status);

        _seatRepositoryMock.Verify(
            x => x.Update(seat1),
            Times.Once);

        _seatRepositoryMock.Verify(
            x => x.Update(seat2),
            Times.Once);

        _parkingRepositoryMock.Verify(
            x => x.Update(parkingSlot),
            Times.Once);

        _bookingRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    // ---------------------------------------------------------
    // HELPERS
    // ---------------------------------------------------------

    private void SetupValidCustomerAndEvent()
    {
        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(3))
            .ReturnsAsync(CreateCustomer());

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(CreateEvent());
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

    private static BookingCreateDto
        CreateBookingCreateDto()
    {
        return new BookingCreateDto
        {
            EventId = 1,
            SeatIds = new List<int>
            {
                1
            },
            ParkingSlotId = null
        };
    }

    private static Customer CreateCustomer()
    {
        return new Customer
        {
            Id = 3,
            FullName = "Test Customer",
            Email = "customer1@test.com",
            Phone = "0771234567",
            PasswordHash = "test-hash",
            Role = "Customer",
            Status = CustomerStatus.Active,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
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
                DateTime.UtcNow.AddDays(10),

            EndDateTime =
                DateTime.UtcNow.AddDays(10)
                    .AddHours(2),

            TicketPrice = 2500,
            ParkingFee = 500,
            Capacity = 300
        };
    }

    private static Seat CreateSeat(
        int id,
        string seatNumber,
        SeatStatus status)
    {
        return new Seat
        {
            Id = id,
            EventId = 1,
            SeatNumber = seatNumber,
            RowLabel = "A",
            ColumnNumber = id,
            SeatType = "Regular",
            PriceOverride = null,
            Status = status
        };
    }

    private static ParkingSlot CreateParkingSlot()
    {
        return new ParkingSlot
        {
            Id = 2,
            EventId = 1,
            SlotNumber = "A2",
            Zone = "Zone A",
            Fee = 500,
            Status = ParkingSlotStatus.Available
        };
    }

    private static Booking CreateBooking()
    {
        var customer =
            CreateCustomer();

        var eventEntity =
            CreateEvent();

        return new Booking
        {
            Id = 1,

            BookingNumber =
                "BKG-TEST-001",

            CustomerId = 3,

            EventId = 1,

            Status =
                BookingStatus.Pending,

            HoldExpiresAt =
                DateTime.UtcNow
                    .AddMinutes(15),

            CreatedAt =
                DateTime.UtcNow,

            Customer =
                customer,

            Event =
                eventEntity,

            BookingSeats =
                new List<BookingSeat>()
        };
    }
}