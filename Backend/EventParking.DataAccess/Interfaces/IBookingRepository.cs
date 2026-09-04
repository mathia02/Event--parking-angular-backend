using EventParking.Models.Entities;

namespace EventParking.DataAccess.Interfaces;

public interface IBookingRepository
{
    Task<List<Booking>> GetAllAsync();

    Task<List<Booking>> GetByCustomerIdAsync(
        int customerId);

    Task<Booking?> GetByIdAsync(int id);

    Task<List<Booking>> GetExpiredPendingBookingsAsync(
        DateTime utcNow);

    Task<bool> BookingNumberExistsAsync(
        string bookingNumber);

    Task AddAsync(Booking booking);

    Task AddBookingSeatsAsync(
        IEnumerable<BookingSeat> bookingSeats);

    Task AddParkingReservationAsync(
        ParkingReservation parkingReservation);

    Task<int> SaveChangesAsync();

    Task ExecuteInTransactionAsync(
        Func<Task> operation);
}