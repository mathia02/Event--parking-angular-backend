using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Payment;
using EventParking.Models.Entities;
using EventParking.Models.Enums;

namespace EventParking.Business.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly IParkingRepository _parkingRepository;
    private readonly INotificationService? _notificationService;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IBookingRepository bookingRepository,
        ISeatRepository seatRepository,
        IParkingRepository parkingRepository,
        INotificationService? notificationService = null)
    {
        _paymentRepository = paymentRepository;
        _bookingRepository = bookingRepository;
        _seatRepository = seatRepository;
        _parkingRepository = parkingRepository;
        _notificationService = notificationService;
    }

    public async Task<List<PaymentDto>> GetAllAsync()
    {
        var payments =
            await _paymentRepository.GetAllAsync();

        return payments
            .Select(MapToDto)
            .ToList();
    }

    public async Task<List<PaymentDto>> GetMyPaymentsAsync(
        int customerId)
    {
        var payments =
            await _paymentRepository
                .GetByCustomerIdAsync(customerId);

        return payments
            .Select(MapToDto)
            .ToList();
    }

    public async Task<PaymentDto> GetByIdAsync(
        int id,
        int requesterCustomerId,
        bool isAdministrator)
    {
        var payment =
            await _paymentRepository.GetByIdAsync(id);

        if (payment == null)
        {
            throw new NotFoundException(
                $"Payment with ID {id} was not found.");
        }

        if (!isAdministrator &&
            payment.CustomerId != requesterCustomerId)
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to view this payment.");
        }

        return MapToDto(payment);
    }

    public async Task<PaymentDto> CreateAsync(
        int bookingId,
        int customerId,
        PaymentCreateDto dto)
    {
        // ---------------------------------------------------------
        // PAYMENT METHOD VALIDATION
        // ---------------------------------------------------------

        if (!Enum.IsDefined(
                typeof(PaymentMethod),
                dto.PaymentMethod) ||
            dto.PaymentMethod ==
            PaymentMethod.NotSpecified)
        {
            throw new ValidationException(
                "Please select a valid payment method.");
        }

        var createdPaymentId = 0;

        await _paymentRepository
            .ExecuteInTransactionAsync(
                async () =>
                {
                    var booking =
                        await _bookingRepository
                            .GetByIdAsync(bookingId);

                    if (booking == null)
                    {
                        throw new NotFoundException(
                            $"Booking with ID {bookingId} was not found.");
                    }

                    if (booking.CustomerId != customerId)
                    {
                        throw new UnauthorizedAccessException(
                            "You can only make payment for your own booking.");
                    }

                    if (booking.Status ==
                        BookingStatus.Cancelled)
                    {
                        throw new ConflictException(
                            "Payment cannot be made for a cancelled booking.");
                    }

                    if (booking.Status ==
                        BookingStatus.Expired)
                    {
                        throw new ConflictException(
                            "Payment cannot be made for an expired booking.");
                    }

                    if (booking.Status ==
                        BookingStatus.Confirmed)
                    {
                        throw new ConflictException(
                            "This booking is already confirmed.");
                    }

                    if (booking.Status !=
                        BookingStatus.Pending)
                    {
                        throw new ConflictException(
                            "Payment can only be made for a pending booking.");
                    }

                    var now =
                        DateTime.UtcNow;

                    if (!booking.HoldExpiresAt.HasValue ||
                        booking.HoldExpiresAt.Value <= now)
                    {
                        throw new ConflictException(
                            "The booking hold has expired.");
                    }

                    var existingPayment =
                        await _paymentRepository
                            .HasPaymentForBookingAsync(
                                bookingId);

                    if (existingPayment)
                    {
                        throw new ConflictException(
                            "A payment already exists for this booking.");
                    }

                    var activeBookingSeats =
                        booking.BookingSeats
                            .Where(x => x.IsActive)
                            .ToList();

                    if (activeBookingSeats.Count == 0)
                    {
                        throw new ValidationException(
                            "The booking does not contain any active seats.");
                    }

                    foreach (var bookingSeat
                             in activeBookingSeats)
                    {
                        if (bookingSeat.Seat == null)
                        {
                            throw new ValidationException(
                                $"Seat information for seat ID {bookingSeat.SeatId} could not be found.");
                        }

                        if (bookingSeat.Seat.Status !=
                            SeatStatus.Held)
                        {
                            throw new ConflictException(
                                $"Seat '{bookingSeat.Seat.SeatNumber}' is not currently held for this booking.");
                        }
                    }

                    var parkingReservation =
                        booking.ParkingReservation;

                    if (parkingReservation != null &&
                        parkingReservation.IsActive)
                    {
                        if (parkingReservation
                                .ParkingSlot == null)
                        {
                            throw new ValidationException(
                                "Parking slot information could not be found.");
                        }

                        if (parkingReservation
                                .ParkingSlot.Status !=
                            ParkingSlotStatus.Held)
                        {
                            throw new ConflictException(
                                $"Parking slot '{parkingReservation.ParkingSlot.SlotNumber}' is not currently held for this booking.");
                        }
                    }

                    // -------------------------------------------------
                    // PAYMENT AMOUNT
                    // -------------------------------------------------

                    var seatTotal =
                        activeBookingSeats.Sum(
                            x => x.PriceAtBooking);

                    var parkingTotal =
                        parkingReservation != null &&
                        parkingReservation.IsActive
                            ? parkingReservation.ReservedFee
                            : 0m;

                    var totalAmount =
                        seatTotal + parkingTotal;

                    if (totalAmount <= 0)
                    {
                        throw new ValidationException(
                            "The payment amount must be greater than zero.");
                    }

                    var transactionReference =
                        await GenerateTransactionReferenceAsync();

                    // -------------------------------------------------
                    // CREATE PAYMENT
                    // -------------------------------------------------

                    var payment =
                        new Payment
                        {
                            BookingId =
                                booking.Id,

                            CustomerId =
                                customerId,

                            Amount =
                                totalAmount,

                            PaymentMethod =
                                dto.PaymentMethod,

                            Status =
                                PaymentStatus.Completed,

                            TransactionReference =
                                transactionReference,

                            CreatedAt =
                                now
                        };

                    await _paymentRepository
                        .AddAsync(payment);

                    // -------------------------------------------------
                    // CONFIRM BOOKING
                    // -------------------------------------------------

                    booking.Status =
                        BookingStatus.Confirmed;

                    booking.ConfirmedAt =
                        now;

                    booking.HoldExpiresAt =
                        null;

                    // -------------------------------------------------
                    // CONFIRM SEATS
                    // -------------------------------------------------

                    foreach (var bookingSeat
                             in activeBookingSeats)
                    {
                        if (bookingSeat.Seat != null)
                        {
                            bookingSeat.Seat.Status =
                                SeatStatus.Booked;

                            _seatRepository.Update(
                                bookingSeat.Seat);
                        }
                    }

                    // -------------------------------------------------
                    // CONFIRM PARKING
                    // -------------------------------------------------

                    if (parkingReservation != null &&
                        parkingReservation.IsActive &&
                        parkingReservation.ParkingSlot != null)
                    {
                        parkingReservation
                            .ParkingSlot.Status =
                            ParkingSlotStatus.Booked;

                        _parkingRepository.Update(
                            parkingReservation.ParkingSlot);
                    }

                    await _paymentRepository
                        .SaveChangesAsync();

                    createdPaymentId =
                        payment.Id;
                });

        // ---------------------------------------------------------
        // RELOAD PAYMENT
        // ---------------------------------------------------------

        var createdPayment =
            await _paymentRepository
                .GetByIdAsync(createdPaymentId);

        if (createdPayment == null)
        {
            throw new NotFoundException(
                "The payment was completed but could not be retrieved.");
        }

        // ---------------------------------------------------------
        // NOTIFICATIONS
        // ---------------------------------------------------------

        if (_notificationService != null)
        {
            await _notificationService
                .CreatePaymentCompletedAsync(
                    createdPayment);

            if (createdPayment.Booking != null)
            {
                await _notificationService
                    .CreateBookingConfirmedAsync(
                        createdPayment.Booking);
            }
        }

        return MapToDto(
            createdPayment);
    }

    private async Task<string>
        GenerateTransactionReferenceAsync()
    {
        for (var attempt = 0;
             attempt < 5;
             attempt++)
        {
            var randomPart =
                Guid.NewGuid()
                    .ToString("N")[..8]
                    .ToUpperInvariant();

            var reference =
                $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{randomPart}";

            var exists =
                await _paymentRepository
                    .TransactionReferenceExistsAsync(
                        reference);

            if (!exists)
            {
                return reference;
            }
        }

        throw new ConflictException(
            "Unable to generate a unique payment transaction reference.");
    }

    private static PaymentDto MapToDto(
        Payment payment)
    {
        return new PaymentDto
        {
            Id =
                payment.Id,

            BookingId =
                payment.BookingId,

            BookingNumber =
                payment.Booking?.BookingNumber ??
                string.Empty,

            CustomerId =
                payment.CustomerId,

            CustomerName =
                payment.Customer?.FullName ??
                string.Empty,

            Amount =
                payment.Amount,

            PaymentMethod =
                payment.PaymentMethod,

            Status =
                payment.Status,

            TransactionReference =
                payment.TransactionReference ??
                string.Empty,

            CreatedAt =
                payment.CreatedAt
        };
    }
}