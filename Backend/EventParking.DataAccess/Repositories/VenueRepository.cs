using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class VenueRepository : IVenueRepository
{
    private readonly ApplicationDbContext _context;

    public VenueRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Venue>> GetAllAsync()
    {
        return await _context.Venues
            .AsNoTracking()
            .OrderBy(v => v.Name)
            .ToListAsync();
    }

    public async Task<Venue?> GetByIdAsync(int id)
    {
        return await _context.Venues
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<List<Venue>> GetAvailableAsync(
        DateTime startDateTime,
        DateTime endDateTime,
        int? excludeEventId = null)
    {
        var venues = _context.Venues
            .AsNoTracking()
            .AsQueryable();

        venues = venues.Where(venue =>
            !_context.Events.Any(e =>
                e.VenueId == venue.Id &&
                (!excludeEventId.HasValue ||
                 e.Id != excludeEventId.Value) &&
                e.StartDateTime < endDateTime &&
                e.EndDateTime > startDateTime));

        return await venues
            .OrderBy(v => v.Name)
            .ToListAsync();
    }

    public async Task<bool> IsAvailableAsync(
        int venueId,
        DateTime startDateTime,
        DateTime endDateTime,
        int? excludeEventId = null)
    {
        var hasOverlap = await _context.Events
            .AsNoTracking()
            .AnyAsync(e =>
                e.VenueId == venueId &&
                (!excludeEventId.HasValue ||
                 e.Id != excludeEventId.Value) &&
                e.StartDateTime < endDateTime &&
                e.EndDateTime > startDateTime);

        return !hasOverlap;
    }

    public async Task<bool> HasUpcomingEventsAsync(
        int venueId,
        DateTime currentDateTime)
    {
        return await _context.Events
            .AsNoTracking()
            .AnyAsync(e =>
                e.VenueId == venueId &&
                e.EndDateTime > currentDateTime);
    }

    public async Task AddAsync(Venue venue)
    {
        await _context.Venues.AddAsync(venue);
    }

    public void Update(Venue venue)
    {
        _context.Venues.Update(venue);
    }

    public void Delete(Venue venue)
    {
        _context.Venues.Remove(venue);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}