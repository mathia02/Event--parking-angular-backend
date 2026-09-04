using EventParking.Models.DTOs.Notification;
using EventParking.Models.Entities;

namespace EventParking.Business.Interfaces;

public interface INotificationService
{
    Task<List<NotificationDto>> GetAllAsync();

    Task<List<NotificationDto>>
        GetMyNotificationsAsync(
            int customerId,
            bool unreadOnly = false);

    Task<NotificationDto> GetByIdAsync(
        int id,
        int requesterCustomerId,
        bool isAdministrator);

    Task<NotificationDto> MarkAsReadAsync(
        int id,
        int requesterCustomerId,
        bool isAdministrator);

    Task CreateBookingCancelledAsync(
        Booking booking);

    Task CreateBookingConfirmedAsync(
        Booking booking);

    Task CreatePaymentCompletedAsync(
        Payment payment);

    Task<int> CreateEventRemindersAsync(
        DateTime fromUtc,
        DateTime toUtc);
}