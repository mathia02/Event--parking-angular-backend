using EventParking.Models.Entities;

namespace EventParking.DataAccess.Interfaces;

public interface ISeatRepository
{
    Task<List<Seat>> GetByEventIdAsync(int eventId);

    Task<List<Seat>> GetAvailableByEventIdAsync(int eventId);

    Task<Seat?> GetByIdAsync(int id);

    Task<int> GetSeatCountByEventAsync(int eventId);

    Task<bool> SeatNumberExistsAsync(
        int eventId,
        string seatNumber,
        int? excludeSeatId = null);

    Task<bool> HasActiveBookingAsync(int seatId);

    Task AddAsync(Seat seat);

    Task AddRangeAsync(IEnumerable<Seat> seats);

    void Update(Seat seat);

    void Delete(Seat seat);

    Task<int> SaveChangesAsync();
}