using EventParking.Models.Entities;

namespace EventParking.DataAccess.Interfaces;

public interface IEventRepository
{
    Task<List<Event>> GetAllAsync(
        string? name,
        DateOnly? date,
        int? venueId,
        int? categoryId);

    Task<Event?> GetByIdAsync(
        int id);

    Task<bool> HasOverlapAsync(
        int venueId,
        DateTime startDateTime,
        DateTime endDateTime,
        int? excludeEventId = null);

    Task<int> GetBookedSeatCountAsync(
        int eventId);

    // ---------------------------------------------------------
    // TOTAL SEAT COUNT
    // Used to keep Event Capacity and Seat Map consistent.
    // ---------------------------------------------------------

    Task<int> GetSeatCountAsync(
        int eventId);

    Task<bool> HasAnyBookingsAsync(
        int eventId);

    Task<bool> HasActiveBookingsAsync(
        int eventId);

    Task AddAsync(
        Event eventEntity);

    void Update(
        Event eventEntity);

    void Delete(
        Event eventEntity);

    Task<int> SaveChangesAsync();

    Task ExecuteInTransactionAsync(
        Func<Task> operation);
}