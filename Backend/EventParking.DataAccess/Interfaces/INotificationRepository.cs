using EventParking.Models.Entities;
using EventParking.Models.Enums;

namespace EventParking.DataAccess.Interfaces;

public interface INotificationRepository
{
    Task<List<Notification>> GetAllAsync();

    Task<List<Notification>> GetByCustomerIdAsync(
        int customerId,
        bool unreadOnly = false);

    Task<Notification?> GetByIdAsync(int id);

    Task<bool> ExistsAsync(
        int customerId,
        int? bookingId,
        int? eventId,
        NotificationType type);

    Task<List<Booking>>
        GetConfirmedBookingsForReminderAsync(
            DateTime fromUtc,
            DateTime toUtc);

    Task AddAsync(Notification notification);

    Task AddRangeAsync(
        IEnumerable<Notification> notifications);

    void Update(Notification notification);

    Task<int> SaveChangesAsync();
}