using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Booking;
using EventParking.Models.Entities;
using EventParking.Models.Enums;

namespace EventParking.Business.Services;

public class BookingService : IBookingService
{
    private const int HoldMinutes = 15;

    private readonly IBookingRepository
        _bookingRepository;

    private readonly ICustomerRepository
        _customerRepository;

    private readonly IEventRepository
        _eventRepository;

    private readonly ISeatRepository
        _seatRepository;

    private readonly IParkingRepository
        _parkingRepository;

    private readonly INotificationService?
        _notificationService;

    public BookingService(
        IBookingRepository bookingRepository,
        ICustomerRepository customerRepository,
        IEventRepository eventRepository,
        ISeatRepository seatRepository,
        IParkingRepository parkingRepository,
        INotificationService?
            notificationService = null)
    {
        _bookingRepository =
            bookingRepository;

        _customerRepository =
            customerRepository;

        _eventRepository =
            eventRepository;

        _seatRepository =
            seatRepository;

        _parkingRepository =
            parkingRepository;

        _notificationService =
            notificationService;
    }

    public async Task<List<BookingDetailsDto>>
        GetAllAsync()
    {
        var bookings =
            await _bookingRepository
                .GetAllAsync();

        return bookings
            .Select(MapToDto)
            .ToList();
    }

    public async Task<List<BookingDetailsDto>>
        GetMyBookingsAsync(
            int customerId)
    {
        var bookings =
            await _bookingRepository
                .GetByCustomerIdAsync(
                    customerId);

        return bookings
            .Select(MapToDto)
            .ToList();
    }

    public async Task<BookingDetailsDto>
        GetByIdAsync(
            int id,
            int requesterCustomerId,
            bool isAdministrator)
    {
        var booking =
            await _bookingRepository
                .GetByIdAsync(id);

        if (booking == null)
        {
            throw new NotFoundException(
                $"Booking with ID {id} was not found.");
        }

        if (!isAdministrator &&
            booking.CustomerId !=
            requesterCustomerId)
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to view this booking.");
        }

        return MapToDto(booking);
    }

    public async Task<BookingDetailsDto>
        CreateAsync(
            int customerId,
            BookingCreateDto dto)
    {
        // ---------------------------------------------------------
        // BASIC VALIDATION
        // ---------------------------------------------------------

        if (dto.EventId <= 0)
        {
            throw new ValidationException(
                "A valid event is required.");
        }

        if (dto.SeatIds == null ||
            dto.SeatIds.Count == 0)
        {
            throw new ValidationException(
                "At least one seat must be selected.");
        }

        if (dto.SeatIds.Any(
                x => x <= 0))
        {
            throw new ValidationException(
                "All seat IDs must be valid.");
        }

        if (dto.SeatIds.Count !=
            dto.SeatIds
                .Distinct()
                .Count())
        {
            throw new ValidationException(
                "The same seat cannot be selected more than once.");
        }

        // ---------------------------------------------------------
        // CUSTOMER
        // ---------------------------------------------------------

        var customer =
            await _customerRepository
                .GetByIdAsync(
                    customerId);

        if (customer == null)
        {
            throw new NotFoundException(
                $"Customer with ID {customerId} was not found.");
        }

        if (!customer.EmailVerified)
        {
            throw new ValidationException(
                "Customer email must be verified before creating a booking.");
        }

        if (customer.Status !=
            CustomerStatus.Active)
        {
            throw new ValidationException(
                "Only active customers can create bookings.");
        }

        // ---------------------------------------------------------
        // EVENT
        // ---------------------------------------------------------

        var eventEntity =
            await _eventRepository
                .GetByIdAsync(
                    dto.EventId);

        if (eventEntity == null)
        {
            throw new NotFoundException(
                $"Event with ID {dto.EventId} was not found.");
        }

        if (eventEntity.StartDateTime <=
            DateTime.UtcNow)
        {
            throw new ValidationException(
                "Bookings can only be created for future events.");
        }

        var createdBookingId = 0;

        // ---------------------------------------------------------
        // TRANSACTION
        // ---------------------------------------------------------

        await _bookingRepository
            .ExecuteInTransactionAsync(
                async () =>
                {
                    var selectedSeats =
                        new List<Seat>();

                    // ---------------------------------------------
                    // SEATS
                    // ---------------------------------------------

                    foreach (var seatId
                             in dto.SeatIds)
                    {
                        var seat =
                            await _seatRepository
                                .GetByIdAsync(
                                    seatId);

                        if (seat == null)
                        {
                            throw new NotFoundException(
                                $"Seat with ID {seatId} was not found.");
                        }

                        if (seat.EventId !=
                            dto.EventId)
                        {
                            throw new ValidationException(
                                $"Seat '{seat.SeatNumber}' does not belong to the selected event.");
                        }

                        if (seat.Status !=
                            SeatStatus.Available)
                        {
                            throw new ConflictException(
                                $"Seat '{seat.SeatNumber}' is not available.");
                        }

                        selectedSeats.Add(
                            seat);
                    }

                    // ---------------------------------------------
                    // PARKING
                    // ---------------------------------------------

                    ParkingSlot? parkingSlot =
                        null;

                    if (dto.ParkingSlotId
                        .HasValue)
                    {
                        parkingSlot =
                            await _parkingRepository
                                .GetByIdAsync(
                                    dto
                                        .ParkingSlotId
                                        .Value);

                        if (parkingSlot ==
                            null)
                        {
                            throw new NotFoundException(
                                $"Parking slot with ID {dto.ParkingSlotId.Value} was not found.");
                        }

                        if (parkingSlot.EventId !=
                            dto.EventId)
                        {
                            throw new ValidationException(
                                "The selected parking slot does not belong to the selected event.");
                        }

                        if (parkingSlot.Status !=
                            ParkingSlotStatus
                                .Available)
                        {
                            throw new ConflictException(
                                $"Parking slot '{parkingSlot.SlotNumber}' is not available.");
                        }
                    }

                    // ---------------------------------------------
                    // BOOKING
                    // ---------------------------------------------

                    var bookingNumber =
                        await GenerateBookingNumberAsync();

                    var now =
                        DateTime.UtcNow;

                    var booking =
                        new Booking
                        {
                            BookingNumber =
                                bookingNumber,

                            CustomerId =
                                customerId,

                            EventId =
                                dto.EventId,

                            Status =
                                BookingStatus.Pending,

                            HoldExpiresAt =
                                now.AddMinutes(
                                    HoldMinutes),

                            CreatedAt =
                                now
                        };

                    await _bookingRepository
                        .AddAsync(
                            booking);

                    // ---------------------------------------------
                    // BOOKING SEATS
                    // ---------------------------------------------

                    var bookingSeats =
                        new List<BookingSeat>();

                    foreach (var seat
                             in selectedSeats)
                    {
                        var seatPrice =
                            seat.PriceOverride ??
                            eventEntity
                                .TicketPrice;

                        if (seatPrice < 0)
                        {
                            throw new ValidationException(
                                $"Seat '{seat.SeatNumber}' has an invalid negative price.");
                        }

                        bookingSeats.Add(
                            new BookingSeat
                            {
                                Booking =
                                    booking,

                                SeatId =
                                    seat.Id,

                                PriceAtBooking =
                                    seatPrice,

                                IsActive =
                                    true,

                                ReservedAt =
                                    now
                            });
                    }

                    await _bookingRepository
                        .AddBookingSeatsAsync(
                            bookingSeats);

                    // ---------------------------------------------
                    // PARKING RESERVATION
                    // ---------------------------------------------

                    ParkingReservation?
                        parkingReservation =
                            null;

                    if (parkingSlot != null)
                    {
                        var reservedFee =
                            parkingSlot.Fee ??
                            eventEntity
                                .ParkingFee;

                        if (reservedFee < 0)
                        {
                            throw new ValidationException(
                                "Parking fee cannot be negative.");
                        }

                        parkingReservation =
                            new ParkingReservation
                            {
                                Booking =
                                    booking,

                                ParkingSlotId =
                                    parkingSlot.Id,

                                ReservedFee =
                                    reservedFee,

                                IsActive =
                                    true,

                                ReservedAt =
                                    now
                            };

                        await _bookingRepository
                            .AddParkingReservationAsync(
                                parkingReservation);
                    }

                    // ---------------------------------------------
                    // TOTAL AMOUNT
                    // ---------------------------------------------

                    var seatTotal =
                        bookingSeats.Sum(
                            x =>
                                x.PriceAtBooking);

                    var parkingTotal =
                        parkingReservation != null
                            ? parkingReservation
                                .ReservedFee
                            : 0m;

                    var totalAmount =
                        seatTotal +
                        parkingTotal;

                    if (totalAmount < 0)
                    {
                        throw new ValidationException(
                            "Booking total amount cannot be negative.");
                    }

                    // ---------------------------------------------
                    // PAID BOOKING
                    // ---------------------------------------------

                    if (totalAmount > 0)
                    {
                        booking.Status =
                            BookingStatus.Pending;

                        booking.HoldExpiresAt =
                            now.AddMinutes(
                                HoldMinutes);

                        booking.ConfirmedAt =
                            null;

                        foreach (var seat
                                 in selectedSeats)
                        {
                            seat.Status =
                                SeatStatus.Held;

                            _seatRepository.Update(
                                seat);
                        }

                        if (parkingSlot !=
                            null)
                        {
                            parkingSlot.Status =
                                ParkingSlotStatus
                                    .Held;

                            _parkingRepository.Update(
                                parkingSlot);
                        }
                    }

                    // ---------------------------------------------
                    // FREE BOOKING
                    // ---------------------------------------------

                    else
                    {
                        booking.Status =
                            BookingStatus
                                .Confirmed;

                        booking.ConfirmedAt =
                            now;

                        booking.HoldExpiresAt =
                            null;

                        foreach (var seat
                                 in selectedSeats)
                        {
                            seat.Status =
                                SeatStatus.Booked;

                            _seatRepository.Update(
                                seat);
                        }

                        if (parkingSlot !=
                            null)
                        {
                            parkingSlot.Status =
                                ParkingSlotStatus
                                    .Booked;

                            _parkingRepository.Update(
                                parkingSlot);
                        }
                    }

                    await _bookingRepository
                        .SaveChangesAsync();

                    createdBookingId =
                        booking.Id;
                });

        var createdBooking =
            await _bookingRepository
                .GetByIdAsync(
                    createdBookingId);

        if (createdBooking == null)
        {
            throw new NotFoundException(
                "The booking was created but could not be retrieved.");
        }

        // Free booking notification
        if (createdBooking.Status ==
                BookingStatus.Confirmed &&
            _notificationService != null)
        {
            await _notificationService
                .CreateBookingConfirmedAsync(
                    createdBooking);
        }

        return MapToDto(
            createdBooking);
    }

    public async Task<BookingDetailsDto>
        CancelAsync(
            int id,
            int requesterCustomerId,
            bool isAdministrator)
    {
        await _bookingRepository
            .ExecuteInTransactionAsync(
                async () =>
                {
                    var booking =
                        await _bookingRepository
                            .GetByIdAsync(id);

                    if (booking == null)
                    {
                        throw new NotFoundException(
                            $"Booking with ID {id} was not found.");
                    }

                    if (!isAdministrator &&
                        booking.CustomerId !=
                        requesterCustomerId)
                    {
                        throw new UnauthorizedAccessException(
                            "You are not allowed to cancel this booking.");
                    }

                    if (booking.Status ==
                        BookingStatus.Cancelled)
                    {
                        throw new ConflictException(
                            "This booking is already cancelled.");
                    }

                    if (booking.Status ==
                        BookingStatus.Expired)
                    {
                        throw new ConflictException(
                            "An expired booking cannot be cancelled.");
                    }

                    if (booking.Event != null &&
                        booking.Event
                            .StartDateTime <=
                        DateTime.UtcNow)
                    {
                        throw new ConflictException(
                            "A booking cannot be cancelled after the event has started.");
                    }

                    // =============================================
                    // STEP 06:
                    // PAID CONFIRMED BOOKING PROTECTION
                    // =============================================

                    if (booking.Status ==
                            BookingStatus.Confirmed &&
                        booking.Payment != null &&
                        booking.Payment.Status ==
                            PaymentStatus.Completed)
                    {
                        throw new ConflictException(
                            "A paid confirmed booking cannot be cancelled because refunds are not supported.");
                    }

                    // Pending booking = allowed
                    // Free confirmed booking = allowed
                    // Paid confirmed booking = blocked above

                    if (booking.Status !=
                            BookingStatus.Pending &&
                        booking.Status !=
                            BookingStatus.Confirmed)
                    {
                        throw new ConflictException(
                            "This booking cannot be cancelled in its current status.");
                    }

                    var now =
                        DateTime.UtcNow;

                    booking.Status =
                        BookingStatus.Cancelled;

                    booking.CancelledAt =
                        now;

                    booking.HoldExpiresAt =
                        null;

                    foreach (
                        var bookingSeat
                        in booking.BookingSeats
                            .Where(
                                x => x.IsActive))
                    {
                        bookingSeat.IsActive =
                            false;

                        bookingSeat.ReleasedAt =
                            now;

                        if (bookingSeat.Seat !=
                            null)
                        {
                            bookingSeat
                                .Seat.Status =
                                SeatStatus
                                    .Available;

                            _seatRepository.Update(
                                bookingSeat.Seat);
                        }
                    }

                    if (booking
                            .ParkingReservation !=
                        null &&
                        booking
                            .ParkingReservation
                            .IsActive)
                    {
                        booking
                            .ParkingReservation
                            .IsActive =
                            false;

                        booking
                            .ParkingReservation
                            .ReleasedAt =
                            now;

                        if (booking
                                .ParkingReservation
                                .ParkingSlot !=
                            null)
                        {
                            booking
                                .ParkingReservation
                                .ParkingSlot!
                                .Status =
                                ParkingSlotStatus
                                    .Available;

                            _parkingRepository.Update(
                                booking
                                    .ParkingReservation
                                    .ParkingSlot!);
                        }
                    }

                    await _bookingRepository
                        .SaveChangesAsync();
                });

        var cancelledBooking =
            await _bookingRepository
                .GetByIdAsync(id);

        if (cancelledBooking == null)
        {
            throw new NotFoundException(
                $"Booking with ID {id} was not found.");
        }

        if (_notificationService != null)
        {
            await _notificationService
                .CreateBookingCancelledAsync(
                    cancelledBooking);
        }

        return MapToDto(
            cancelledBooking);
    }

    private async Task<string>
        GenerateBookingNumberAsync()
    {
        for (var attempt = 0;
             attempt < 5;
             attempt++)
        {
            var randomPart =
                Guid.NewGuid()
                    .ToString("N")[..8]
                    .ToUpperInvariant();

            var bookingNumber =
                $"BKG-{DateTime.UtcNow:yyyyMMddHHmmss}-{randomPart}";

            var exists =
                await _bookingRepository
                    .BookingNumberExistsAsync(
                        bookingNumber);

            if (!exists)
            {
                return bookingNumber;
            }
        }

        throw new ConflictException(
            "Unable to generate a unique booking number.");
    }

    private static BookingDetailsDto
        MapToDto(
            Booking booking)
    {
        var activeSeats =
            booking.BookingSeats
                .Where(
                    x => x.IsActive)
                .ToList();

        var seatTotal =
            activeSeats.Sum(
                x =>
                    x.PriceAtBooking);

        var parkingTotal =
            booking.ParkingReservation !=
                null &&
            booking.ParkingReservation
                .IsActive
                ? booking
                    .ParkingReservation
                    .ReservedFee
                : 0m;

        return new BookingDetailsDto
        {
            Id =
                booking.Id,

            BookingNumber =
                booking.BookingNumber,

            CustomerId =
                booking.CustomerId,

            CustomerName =
                booking.Customer
                    ?.FullName ??
                string.Empty,

            EventId =
                booking.EventId,

            EventName =
                booking.Event
                    ?.Name ??
                string.Empty,

            Status =
                booking.Status,

            HoldExpiresAt =
                booking.HoldExpiresAt,

            CreatedAt =
                booking.CreatedAt,

            ConfirmedAt =
                booking.ConfirmedAt,

            CancelledAt =
                booking.CancelledAt,

            Seats =
                activeSeats
                    .Select(
                        x =>
                            new BookingSeatItemDto
                            {
                                SeatId =
                                    x.SeatId,

                                SeatNumber =
                                    x.Seat
                                        ?.SeatNumber ??
                                    string.Empty,

                                SeatType =
                                    x.Seat
                                        ?.SeatType ??
                                    string.Empty,

                                PriceAtBooking =
                                    x.PriceAtBooking
                            })
                    .ToList(),

            Parking =
                booking.ParkingReservation !=
                    null &&
                booking.ParkingReservation
                    .IsActive
                    ? new BookingParkingDto
                    {
                        ParkingSlotId =
                            booking
                                .ParkingReservation
                                .ParkingSlotId,

                        SlotNumber =
                            booking
                                .ParkingReservation
                                .ParkingSlot
                                ?.SlotNumber ??
                            string.Empty,

                        Zone =
                            booking
                                .ParkingReservation
                                .ParkingSlot
                                ?.Zone ??
                            string.Empty,

                        ReservedFee =
                            booking
                                .ParkingReservation
                                .ReservedFee
                    }
                    : null,

            TotalAmount =
                seatTotal +
                parkingTotal
        };
    }
}