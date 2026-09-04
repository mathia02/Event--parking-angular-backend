using EventParking.Business.Exceptions;
using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Payment;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Moq;
using Xunit;

namespace EventParking.Tests.Services;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<ISeatRepository> _seatRepositoryMock;
    private readonly Mock<IParkingRepository> _parkingRepositoryMock;

    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _paymentRepositoryMock =
            new Mock<IPaymentRepository>();

        _bookingRepositoryMock =
            new Mock<IBookingRepository>();

        _seatRepositoryMock =
            new Mock<ISeatRepository>();

        _parkingRepositoryMock =
            new Mock<IParkingRepository>();

        _paymentService =
            new PaymentService(
                _paymentRepositoryMock.Object,
                _bookingRepositoryMock.Object,
                _seatRepositoryMock.Object,
                _parkingRepositoryMock.Object);
    }

    // ---------------------------------------------------------
    // GET ALL
    // ---------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_ShouldReturnPayments()
    {
        var payment =
            CreatePayment();

        _paymentRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                new List<Payment>
                {
                    payment
                });

        var result =
            await _paymentService.GetAllAsync();

        Assert.Single(result);

        Assert.Equal(
            1,
            result[0].Id);

        Assert.Equal(
            3000m,
            result[0].Amount);

        Assert.Equal(
            PaymentMethod.Card,
            result[0].PaymentMethod);

        Assert.Equal(
            PaymentStatus.Completed,
            result[0].Status);
    }

    // ---------------------------------------------------------
    // GET MY PAYMENTS
    // ---------------------------------------------------------

    [Fact]
    public async Task GetMyPaymentsAsync_ShouldReturnCustomerPayments()
    {
        var payment =
            CreatePayment();

        _paymentRepositoryMock
            .Setup(x =>
                x.GetByCustomerIdAsync(3))
            .ReturnsAsync(
                new List<Payment>
                {
                    payment
                });

        var result =
            await _paymentService
                .GetMyPaymentsAsync(3);

        Assert.Single(result);

        Assert.Equal(
            3,
            result[0].CustomerId);

        Assert.Equal(
            PaymentMethod.Card,
            result[0].PaymentMethod);

        _paymentRepositoryMock.Verify(
            x => x.GetByCustomerIdAsync(3),
            Times.Once);
    }

    // ---------------------------------------------------------
    // GET BY ID
    // ---------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_WhenPaymentDoesNotExist_ShouldThrowNotFoundException()
    {
        _paymentRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(999))
            .ReturnsAsync(
                (Payment?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () =>
                _paymentService.GetByIdAsync(
                    999,
                    3,
                    false));
    }

    [Fact]
    public async Task GetByIdAsync_WhenAnotherCustomerRequestsPayment_ShouldThrowUnauthorizedAccessException()
    {
        var payment =
            CreatePayment();

        _paymentRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(payment);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () =>
                _paymentService.GetByIdAsync(
                    1,
                    999,
                    false));
    }

    [Fact]
    public async Task GetByIdAsync_WhenAdministratorRequestsPayment_ShouldReturnPayment()
    {
        var payment =
            CreatePayment();

        _paymentRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(payment);

        var result =
            await _paymentService
                .GetByIdAsync(
                    1,
                    999,
                    true);

        Assert.Equal(
            1,
            result.Id);

        Assert.Equal(
            3,
            result.CustomerId);

        Assert.Equal(
            PaymentMethod.Card,
            result.PaymentMethod);
    }

    // ---------------------------------------------------------
    // PAYMENT METHOD VALIDATION
    // ---------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WhenPaymentMethodIsNotSpecified_ShouldThrowValidationException()
    {
        var dto =
            new PaymentCreateDto
            {
                PaymentMethod =
                    PaymentMethod.NotSpecified
            };

        await Assert.ThrowsAsync<ValidationException>(
            () =>
                _paymentService.CreateAsync(
                    1,
                    3,
                    dto));

        _paymentRepositoryMock.Verify(
            x =>
                x.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenPaymentMethodIsInvalid_ShouldThrowValidationException()
    {
        var dto =
            new PaymentCreateDto
            {
                PaymentMethod =
                    (PaymentMethod)999
            };

        await Assert.ThrowsAsync<ValidationException>(
            () =>
                _paymentService.CreateAsync(
                    1,
                    3,
                    dto));

        _paymentRepositoryMock.Verify(
            x =>
                x.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>()),
            Times.Never);
    }

    // ---------------------------------------------------------
    // BOOKING VALIDATION
    // ---------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WhenBookingDoesNotExist_ShouldThrowNotFoundException()
    {
        SetupTransactionExecution();

        _bookingRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(999))
            .ReturnsAsync(
                (Booking?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () =>
                _paymentService.CreateAsync(
                    999,
                    3,
                    CreateValidPaymentDto()));
    }

    [Fact]
    public async Task CreateAsync_WhenCustomerDoesNotOwnBooking_ShouldThrowUnauthorizedAccessException()
    {
        var booking =
            CreatePendingBooking();

        booking.CustomerId =
            10;

        SetupTransactionExecution();

        _bookingRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(booking);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () =>
                _paymentService.CreateAsync(
                    1,
                    3,
                    CreateValidPaymentDto()));
    }

    [Fact]
    public async Task CreateAsync_WhenBookingIsCancelled_ShouldThrowConflictException()
    {
        var booking =
            CreatePendingBooking();

        booking.Status =
            BookingStatus.Cancelled;

        SetupTransactionExecution();

        _bookingRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(booking);

        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _paymentService.CreateAsync(
                    1,
                    3,
                    CreateValidPaymentDto()));
    }

    [Fact]
    public async Task CreateAsync_WhenBookingIsExpired_ShouldThrowConflictException()
    {
        var booking =
            CreatePendingBooking();

        booking.Status =
            BookingStatus.Expired;

        SetupTransactionExecution();

        _bookingRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(booking);

        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _paymentService.CreateAsync(
                    1,
                    3,
                    CreateValidPaymentDto()));
    }

    [Fact]
    public async Task CreateAsync_WhenBookingIsAlreadyConfirmed_ShouldThrowConflictException()
    {
        var booking =
            CreatePendingBooking();

        booking.Status =
            BookingStatus.Confirmed;

        SetupTransactionExecution();

        _bookingRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(booking);

        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _paymentService.CreateAsync(
                    1,
                    3,
                    CreateValidPaymentDto()));
    }

    [Fact]
    public async Task CreateAsync_WhenBookingHoldExpired_ShouldThrowConflictException()
    {
        var booking =
            CreatePendingBooking();

        booking.HoldExpiresAt =
            DateTime.UtcNow
                .AddMinutes(-1);

        SetupTransactionExecution();

        _bookingRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(booking);

        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _paymentService.CreateAsync(
                    1,
                    3,
                    CreateValidPaymentDto()));
    }

    // ---------------------------------------------------------
    // DUPLICATE PAYMENT
    // ---------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WhenPaymentAlreadyExists_ShouldThrowConflictException()
    {
        var booking =
            CreatePendingBooking();

        SetupTransactionExecution();

        _bookingRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(booking);

        _paymentRepositoryMock
            .Setup(x =>
                x.HasPaymentForBookingAsync(1))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _paymentService.CreateAsync(
                    1,
                    3,
                    CreateValidPaymentDto()));

        _paymentRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<Payment>()),
            Times.Never);
    }

    // ---------------------------------------------------------
    // SEAT VALIDATION
    // ---------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WhenBookingHasNoActiveSeats_ShouldThrowValidationException()
    {
        var booking =
            CreatePendingBooking();

        booking.BookingSeats =
            new List<BookingSeat>();

        SetupValidPaymentStart(
            booking);

        await Assert.ThrowsAsync<ValidationException>(
            () =>
                _paymentService.CreateAsync(
                    1,
                    3,
                    CreateValidPaymentDto()));
    }

    [Fact]
    public async Task CreateAsync_WhenSeatInformationMissing_ShouldThrowValidationException()
    {
        var booking =
            CreatePendingBooking();

        booking.BookingSeats =
            new List<BookingSeat>
            {
                new BookingSeat
                {
                    Id = 1,
                    BookingId = 1,
                    SeatId = 3,
                    PriceAtBooking = 2500,
                    IsActive = true,
                    ReservedAt = DateTime.UtcNow,
                    Seat = null
                }
            };

        SetupValidPaymentStart(
            booking);

        await Assert.ThrowsAsync<ValidationException>(
            () =>
                _paymentService.CreateAsync(
                    1,
                    3,
                    CreateValidPaymentDto()));
    }

    [Fact]
    public async Task CreateAsync_WhenSeatIsNotHeld_ShouldThrowConflictException()
    {
        var booking =
            CreatePendingBooking();

        var seat =
            CreateSeat();

        seat.Status =
            SeatStatus.Available;

        booking.BookingSeats =
            new List<BookingSeat>
            {
                new BookingSeat
                {
                    Id = 1,
                    BookingId = 1,
                    SeatId = seat.Id,
                    PriceAtBooking = 2500,
                    IsActive = true,
                    ReservedAt = DateTime.UtcNow,
                    Seat = seat
                }
            };

        SetupValidPaymentStart(
            booking);

        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _paymentService.CreateAsync(
                    1,
                    3,
                    CreateValidPaymentDto()));
    }

    // ---------------------------------------------------------
    // PARKING VALIDATION
    // ---------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WhenParkingIsNotHeld_ShouldThrowConflictException()
    {
        var booking =
            CreatePendingBooking();

        var parkingSlot =
            CreateParkingSlot();

        parkingSlot.Status =
            ParkingSlotStatus.Available;

        booking.ParkingReservation =
            new ParkingReservation
            {
                Id = 1,
                BookingId = 1,
                ParkingSlotId = 2,
                ReservedFee = 500,
                IsActive = true,
                ReservedAt = DateTime.UtcNow,
                ParkingSlot = parkingSlot
            };

        SetupValidPaymentStart(
            booking);

        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _paymentService.CreateAsync(
                    1,
                    3,
                    CreateValidPaymentDto()));
    }

    // ---------------------------------------------------------
    // SUCCESS - PAYMENT METHODS
    // ---------------------------------------------------------

    [Theory]
    [InlineData(PaymentMethod.Card)]
    [InlineData(PaymentMethod.BankTransfer)]
    [InlineData(PaymentMethod.MobileWallet)]
    public async Task CreateAsync_WithValidPaymentMethod_ShouldStoreAndReturnSelectedPaymentMethod(
        PaymentMethod paymentMethod)
    {
        var booking =
            CreatePendingBooking();

        SetupSuccessfulPayment(
            booking,
            paymentId: 10);

        var result =
            await _paymentService.CreateAsync(
                1,
                3,
                CreateValidPaymentDto(
                    paymentMethod));

        Assert.Equal(
            paymentMethod,
            result.PaymentMethod);

        _paymentRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.Is<Payment>(
                        p =>
                            p.PaymentMethod ==
                            paymentMethod)),
            Times.Once);
    }

    // ---------------------------------------------------------
    // SUCCESS WITH PARKING
    // ---------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WithValidBooking_ShouldCompletePaymentConfirmBookingAndBookResources()
    {
        var booking =
            CreatePendingBooking();

        var seat =
            booking.BookingSeats
                .First()
                .Seat!;

        var parkingSlot =
            booking.ParkingReservation!
                .ParkingSlot!;

        SetupTransactionExecution();

        _bookingRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(booking);

        _paymentRepositoryMock
            .Setup(x =>
                x.HasPaymentForBookingAsync(1))
            .ReturnsAsync(false);

        _paymentRepositoryMock
            .Setup(x =>
                x.TransactionReferenceExistsAsync(
                    It.IsAny<string>()))
            .ReturnsAsync(false);

        Payment? capturedPayment =
            null;

        _paymentRepositoryMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<Payment>()))
            .Callback<Payment>(
                payment =>
                {
                    payment.Id = 1;

                    capturedPayment =
                        payment;
                })
            .Returns(Task.CompletedTask);

        _paymentRepositoryMock
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        _paymentRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(() =>
            {
                if (capturedPayment == null)
                {
                    return null;
                }

                capturedPayment.Booking =
                    booking;

                capturedPayment.Customer =
                    booking.Customer;

                return capturedPayment;
            });

        var result =
            await _paymentService.CreateAsync(
                1,
                3,
                CreateValidPaymentDto(
                    PaymentMethod.Card));

        Assert.Equal(
            1,
            result.Id);

        Assert.Equal(
            3000m,
            result.Amount);

        Assert.Equal(
            PaymentMethod.Card,
            result.PaymentMethod);

        Assert.Equal(
            PaymentStatus.Completed,
            result.Status);

        Assert.StartsWith(
            "PAY-",
            result.TransactionReference);

        Assert.Equal(
            BookingStatus.Confirmed,
            booking.Status);

        Assert.NotNull(
            booking.ConfirmedAt);

        Assert.Null(
            booking.HoldExpiresAt);

        Assert.Equal(
            SeatStatus.Booked,
            seat.Status);

        Assert.Equal(
            ParkingSlotStatus.Booked,
            parkingSlot.Status);

        _seatRepositoryMock.Verify(
            x => x.Update(seat),
            Times.Once);

        _parkingRepositoryMock.Verify(
            x => x.Update(parkingSlot),
            Times.Once);

        _paymentRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    // ---------------------------------------------------------
    // SUCCESS WITHOUT PARKING
    // ---------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WithoutParking_ShouldCalculateSeatTotalOnly()
    {
        var booking =
            CreatePendingBooking();

        booking.ParkingReservation =
            null;

        var seat =
            booking.BookingSeats
                .First()
                .Seat!;

        SetupTransactionExecution();

        _bookingRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(booking);

        _paymentRepositoryMock
            .Setup(x =>
                x.HasPaymentForBookingAsync(1))
            .ReturnsAsync(false);

        _paymentRepositoryMock
            .Setup(x =>
                x.TransactionReferenceExistsAsync(
                    It.IsAny<string>()))
            .ReturnsAsync(false);

        Payment? capturedPayment =
            null;

        _paymentRepositoryMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<Payment>()))
            .Callback<Payment>(
                payment =>
                {
                    payment.Id = 2;

                    capturedPayment =
                        payment;
                })
            .Returns(Task.CompletedTask);

        _paymentRepositoryMock
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        _paymentRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(2))
            .ReturnsAsync(() =>
            {
                if (capturedPayment == null)
                {
                    return null;
                }

                capturedPayment.Booking =
                    booking;

                capturedPayment.Customer =
                    booking.Customer;

                return capturedPayment;
            });

        var result =
            await _paymentService.CreateAsync(
                1,
                3,
                CreateValidPaymentDto(
                    PaymentMethod.MobileWallet));

        Assert.Equal(
            2500m,
            result.Amount);

        Assert.Equal(
            PaymentMethod.MobileWallet,
            result.PaymentMethod);

        Assert.Equal(
            PaymentStatus.Completed,
            result.Status);

        Assert.Equal(
            BookingStatus.Confirmed,
            booking.Status);

        Assert.Equal(
            SeatStatus.Booked,
            seat.Status);

        _parkingRepositoryMock.Verify(
            x =>
                x.Update(
                    It.IsAny<ParkingSlot>()),
            Times.Never);
    }

    // ---------------------------------------------------------
    // HELPERS
    // ---------------------------------------------------------

    private void SetupValidPaymentStart(
        Booking booking)
    {
        SetupTransactionExecution();

        _bookingRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(booking);

        _paymentRepositoryMock
            .Setup(x =>
                x.HasPaymentForBookingAsync(1))
            .ReturnsAsync(false);
    }

    private void SetupSuccessfulPayment(
        Booking booking,
        int paymentId)
    {
        SetupTransactionExecution();

        _bookingRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(booking);

        _paymentRepositoryMock
            .Setup(x =>
                x.HasPaymentForBookingAsync(1))
            .ReturnsAsync(false);

        _paymentRepositoryMock
            .Setup(x =>
                x.TransactionReferenceExistsAsync(
                    It.IsAny<string>()))
            .ReturnsAsync(false);

        Payment? capturedPayment =
            null;

        _paymentRepositoryMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<Payment>()))
            .Callback<Payment>(
                payment =>
                {
                    payment.Id =
                        paymentId;

                    capturedPayment =
                        payment;
                })
            .Returns(Task.CompletedTask);

        _paymentRepositoryMock
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        _paymentRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(
                    paymentId))
            .ReturnsAsync(() =>
            {
                if (capturedPayment == null)
                {
                    return null;
                }

                capturedPayment.Booking =
                    booking;

                capturedPayment.Customer =
                    booking.Customer;

                return capturedPayment;
            });
    }

    private void SetupTransactionExecution()
    {
        _paymentRepositoryMock
            .Setup(x =>
                x.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>()))
            .Returns(
                (Func<Task> operation) =>
                    operation());
    }

    private static PaymentCreateDto
        CreateValidPaymentDto(
            PaymentMethod paymentMethod =
                PaymentMethod.Card)
    {
        return new PaymentCreateDto
        {
            PaymentMethod =
                paymentMethod
        };
    }

    private static Customer CreateCustomer()
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
                "test-hash",

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

    private static Seat CreateSeat()
    {
        return new Seat
        {
            Id = 3,

            EventId = 1,

            SeatNumber =
                "A3",

            RowLabel =
                "A",

            ColumnNumber =
                3,

            SeatType =
                "Regular",

            PriceOverride =
                null,

            Status =
                SeatStatus.Held
        };
    }

    private static ParkingSlot
        CreateParkingSlot()
    {
        return new ParkingSlot
        {
            Id = 2,

            EventId = 1,

            SlotNumber =
                "A2",

            Zone =
                "Zone A",

            Fee =
                500,

            Status =
                ParkingSlotStatus.Held
        };
    }

    private static Booking
        CreatePendingBooking()
    {
        var customer =
            CreateCustomer();

        var seat =
            CreateSeat();

        var parkingSlot =
            CreateParkingSlot();

        var booking =
            new Booking
            {
                Id = 1,

                BookingNumber =
                    "BKG-PAYMENT-TEST-001",

                CustomerId =
                    3,

                EventId =
                    1,

                Status =
                    BookingStatus.Pending,

                HoldExpiresAt =
                    DateTime.UtcNow
                        .AddMinutes(15),

                CreatedAt =
                    DateTime.UtcNow,

                Customer =
                    customer
            };

        var bookingSeat =
            new BookingSeat
            {
                Id = 1,

                BookingId =
                    1,

                SeatId =
                    3,

                PriceAtBooking =
                    2500,

                IsActive =
                    true,

                ReservedAt =
                    DateTime.UtcNow,

                Booking =
                    booking,

                Seat =
                    seat
            };

        var parkingReservation =
            new ParkingReservation
            {
                Id = 1,

                BookingId =
                    1,

                ParkingSlotId =
                    2,

                ReservedFee =
                    500,

                IsActive =
                    true,

                ReservedAt =
                    DateTime.UtcNow,

                Booking =
                    booking,

                ParkingSlot =
                    parkingSlot
            };

        booking.BookingSeats =
            new List<BookingSeat>
            {
                bookingSeat
            };

        booking.ParkingReservation =
            parkingReservation;

        return booking;
    }

    private static Payment CreatePayment()
    {
        var customer =
            CreateCustomer();

        var booking =
            CreatePendingBooking();

        booking.Status =
            BookingStatus.Confirmed;

        return new Payment
        {
            Id = 1,

            BookingId =
                1,

            CustomerId =
                3,

            Amount =
                3000,

            PaymentMethod =
                PaymentMethod.Card,

            Status =
                PaymentStatus.Completed,

            TransactionReference =
                "PAY-TEST-001",

            CreatedAt =
                DateTime.UtcNow,

            Booking =
                booking,

            Customer =
                customer
        };
    }
}