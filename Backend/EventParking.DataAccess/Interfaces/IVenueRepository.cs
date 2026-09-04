using EventParking.Models.Entities;

namespace EventParking.DataAccess.Interfaces;

public interface IVenueRepository
{
    Task<List<Venue>> GetAllAsync();

    Task<Venue?> GetByIdAsync(int id);

    Task<List<Venue>> GetAvailableAsync(
        DateTime startDateTime,
        DateTime endDateTime,
        int? excludeEventId = null);

    Task<bool> IsAvailableAsync(
        int venueId,
        DateTime startDateTime,
        DateTime endDateTime,
        int? excludeEventId = null);

    Task<bool> HasUpcomingEventsAsync(
        int venueId,
        DateTime currentDateTime);

    Task AddAsync(Venue venue);

    void Update(Venue venue);

    void Delete(Venue venue);

    Task<int> SaveChangesAsync();
}