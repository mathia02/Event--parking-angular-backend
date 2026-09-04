using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Enums;

namespace EventParking.Business.Services;

public class BookingExpiryService
    : IBookingExpiryService
{
    private readonly IBookingRepository
        _bookingRepository;

    private readonly ISeatRepository
        _seatRepository;

    private readonly IParkingRepository
        _parkingRepository;

    public BookingExpiryService(
        IBookingRepository bookingRepository,
        ISeatRepository seatRepository,
        IParkingRepository parkingRepository)
    {
        _bookingRepository =
            bookingRepository;

        _seatRepository =
            seatRepository;

        _parkingRepository =
            parkingRepository;
    }

    public async Task<int>
        ExpirePendingBookingsAsync()
    {
        var expiredCount = 0;

        await _bookingRepository
            .ExecuteInTransactionAsync(
                async () =>
                {
                    var now =
                        DateTime.UtcNow;

                    var bookings =
                        await _bookingRepository
                            .GetExpiredPendingBookingsAsync(
                                now);

                    foreach (var booking in bookings)
                    {
                        // Extra protection:
                        // Only Pending bookings should expire.
                        if (booking.Status !=
                            BookingStatus.Pending)
                        {
                            continue;
                        }

                        if (!booking.HoldExpiresAt.HasValue ||
                            booking.HoldExpiresAt.Value > now)
                        {
                            continue;
                        }

                        booking.Status =
                            BookingStatus.Expired;

                        // Release held seats
                        foreach (
                            var bookingSeat
                            in booking.BookingSeats
                                .Where(x => x.IsActive))
                        {
                            bookingSeat.IsActive =
                                false;

                            bookingSeat.ReleasedAt =
                                now;

                            if (bookingSeat.Seat != null &&
                                bookingSeat.Seat.Status ==
                                SeatStatus.Held)
                            {
                                bookingSeat.Seat.Status =
                                    SeatStatus.Available;

                                _seatRepository.Update(
                                    bookingSeat.Seat);
                            }
                        }

                        // Release held parking
                        if (booking.ParkingReservation != null &&
                            booking.ParkingReservation.IsActive)
                        {
                            booking.ParkingReservation.IsActive =
                                false;

                            booking.ParkingReservation.ReleasedAt =
                                now;

                            var parkingSlot =
                                booking
                                    .ParkingReservation
                                    .ParkingSlot;

                            if (parkingSlot != null &&
                                parkingSlot.Status ==
                                ParkingSlotStatus.Held)
                            {
                                parkingSlot.Status =
                                    ParkingSlotStatus.Available;

                                _parkingRepository.Update(
                                    parkingSlot);
                            }
                        }

                        expiredCount++;
                    }

                    if (expiredCount > 0)
                    {
                        await _bookingRepository
                            .SaveChangesAsync();
                    }
                });

        return expiredCount;
    }
}