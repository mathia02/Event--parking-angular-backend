using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class EventRepository : IEventRepository
{
    private readonly ApplicationDbContext _context;

    public EventRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Event>> GetAllAsync(
        string? name,
        DateOnly? date,
        int? venueId,
        int? categoryId)
    {
        var query =
            _context.Events
                .AsNoTracking()
                .Include(e => e.Venue)
                .Include(e => e.Category)
                .AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            var search =
                name.Trim();

            query =
                query.Where(e =>
                    e.Name.Contains(search));
        }

        if (date.HasValue)
        {
            var dayStart =
                date.Value.ToDateTime(
                    TimeOnly.MinValue);

            var nextDay =
                dayStart.AddDays(1);

            query =
                query.Where(e =>
                    e.StartDateTime >= dayStart &&
                    e.StartDateTime < nextDay);
        }

        if (venueId.HasValue)
        {
            query =
                query.Where(e =>
                    e.VenueId ==
                    venueId.Value);
        }

        if (categoryId.HasValue)
        {
            query =
                query.Where(e =>
                    e.CategoryId ==
                    categoryId.Value);
        }

        return await query
            .OrderBy(e =>
                e.StartDateTime)
            .ToListAsync();
    }

    public async Task<Event?>
        GetByIdAsync(
            int id)
    {
        return await _context.Events
            .Include(e => e.Venue)
            .Include(e => e.Category)
            .FirstOrDefaultAsync(
                e => e.Id == id);
    }

    public async Task<bool> HasOverlapAsync(
        int venueId,
        DateTime startDateTime,
        DateTime endDateTime,
        int? excludeEventId = null)
    {
        return await _context.Events
            .AsNoTracking()
            .AnyAsync(e =>
                e.VenueId == venueId &&
                (
                    !excludeEventId.HasValue ||
                    e.Id != excludeEventId.Value
                ) &&
                e.StartDateTime < endDateTime &&
                e.EndDateTime > startDateTime);
    }

    // ---------------------------------------------------------
    // ACTIVE / BOOKED SEAT COUNT
    // ---------------------------------------------------------

    public async Task<int>
        GetBookedSeatCountAsync(
            int eventId)
    {
        return await _context.BookingSeats
            .AsNoTracking()
            .CountAsync(bs =>
                bs.IsActive &&
                bs.Booking != null &&
                bs.Booking.EventId ==
                    eventId &&
                (
                    bs.Booking.Status ==
                        BookingStatus.Pending ||
                    bs.Booking.Status ==
                        BookingStatus.Confirmed
                ));
    }

    // ---------------------------------------------------------
    // TOTAL SEAT MAP COUNT
    // ---------------------------------------------------------

    public async Task<int>
        GetSeatCountAsync(
            int eventId)
    {
        return await _context.Seats
            .AsNoTracking()
            .CountAsync(seat =>
                seat.EventId ==
                eventId);
    }

    // ---------------------------------------------------------
    // BOOKINGS
    // ---------------------------------------------------------

    public async Task<bool>
        HasAnyBookingsAsync(
            int eventId)
    {
        return await _context.Bookings
            .AsNoTracking()
            .AnyAsync(b =>
                b.EventId ==
                eventId);
    }

    public async Task<bool>
        HasActiveBookingsAsync(
            int eventId)
    {
        return await _context.Bookings
            .AsNoTracking()
            .AnyAsync(b =>
                b.EventId == eventId &&
                (
                    b.Status ==
                        BookingStatus.Pending ||
                    b.Status ==
                        BookingStatus.Confirmed
                ));
    }

    // ---------------------------------------------------------
    // CREATE
    // ---------------------------------------------------------

    public async Task AddAsync(
        Event eventEntity)
    {
        await _context.Events
            .AddAsync(eventEntity);
    }

    // ---------------------------------------------------------
    // UPDATE
    // ---------------------------------------------------------

    public void Update(
        Event eventEntity)
    {
        _context.Events
            .Update(eventEntity);
    }

    // ---------------------------------------------------------
    // DELETE
    // ---------------------------------------------------------

    public void Delete(
        Event eventEntity)
    {
        _context.Events
            .Remove(eventEntity);
    }

    // ---------------------------------------------------------
    // SAVE
    // ---------------------------------------------------------

    public async Task<int>
        SaveChangesAsync()
    {
        return await _context
            .SaveChangesAsync();
    }

    // ---------------------------------------------------------
    // TRANSACTION
    // ---------------------------------------------------------

    public async Task
        ExecuteInTransactionAsync(
            Func<Task> operation)
    {
        await using var transaction =
            await _context.Database
                .BeginTransactionAsync();

        try
        {
            await operation();

            await transaction
                .CommitAsync();
        }
        catch
        {
            await transaction
                .RollbackAsync();

            throw;
        }
    }
}