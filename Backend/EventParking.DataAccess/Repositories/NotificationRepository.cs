using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class NotificationRepository
    : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Notification>> GetAllAsync()
    {
        return await _context.Notifications
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Notification>>
        GetByCustomerIdAsync(
            int customerId,
            bool unreadOnly = false)
    {
        var query =
            _context.Notifications
                .AsNoTracking()
                .Where(x =>
                    x.CustomerId == customerId);

        if (unreadOnly)
        {
            query =
                query.Where(x => !x.IsRead);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<Notification?>
        GetByIdAsync(int id)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> ExistsAsync(
        int customerId,
        int? bookingId,
        int? eventId,
        NotificationType type)
    {
        return await _context.Notifications
            .AnyAsync(x =>
                x.CustomerId == customerId &&
                x.BookingId == bookingId &&
                x.EventId == eventId &&
                x.Type == type);
    }

    public async Task<List<Booking>>
        GetConfirmedBookingsForReminderAsync(
            DateTime fromUtc,
            DateTime toUtc)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Include(x => x.Event)
            .Where(x =>
                x.Status == BookingStatus.Confirmed &&
                x.Event != null &&
                x.Event.StartDateTime >= fromUtc &&
                x.Event.StartDateTime <= toUtc)
            .ToListAsync();
    }

    public async Task AddAsync(
        Notification notification)
    {
        await _context.Notifications
            .AddAsync(notification);
    }

    public async Task AddRangeAsync(
        IEnumerable<Notification> notifications)
    {
        await _context.Notifications
            .AddRangeAsync(notifications);
    }

    public void Update(
        Notification notification)
    {
        _context.Notifications.Update(notification);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}