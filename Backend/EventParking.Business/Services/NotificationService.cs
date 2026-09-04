using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Notification;
using EventParking.Models.Entities;
using EventParking.Models.Enums;

namespace EventParking.Business.Services;

public class NotificationService
    : INotificationService
{
    private readonly INotificationRepository
        _notificationRepository;

    public NotificationService(
        INotificationRepository notificationRepository)
    {
        _notificationRepository =
            notificationRepository;
    }

    public async Task<List<NotificationDto>>
        GetAllAsync()
    {
        var notifications =
            await _notificationRepository
                .GetAllAsync();

        return notifications
            .Select(MapToDto)
            .ToList();
    }

    public async Task<List<NotificationDto>>
        GetMyNotificationsAsync(
            int customerId,
            bool unreadOnly = false)
    {
        var notifications =
            await _notificationRepository
                .GetByCustomerIdAsync(
                    customerId,
                    unreadOnly);

        return notifications
            .Select(MapToDto)
            .ToList();
    }

    public async Task<NotificationDto>
        GetByIdAsync(
            int id,
            int requesterCustomerId,
            bool isAdministrator)
    {
        var notification =
            await _notificationRepository
                .GetByIdAsync(id);

        if (notification == null)
        {
            throw new NotFoundException(
                $"Notification with ID {id} was not found.");
        }

        if (!isAdministrator &&
            notification.CustomerId !=
            requesterCustomerId)
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to view this notification.");
        }

        return MapToDto(notification);
    }

    public async Task<NotificationDto>
        MarkAsReadAsync(
            int id,
            int requesterCustomerId,
            bool isAdministrator)
    {
        var notification =
            await _notificationRepository
                .GetByIdAsync(id);

        if (notification == null)
        {
            throw new NotFoundException(
                $"Notification with ID {id} was not found.");
        }

        if (!isAdministrator &&
            notification.CustomerId !=
            requesterCustomerId)
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to update this notification.");
        }

        notification.IsRead = true;

        _notificationRepository.Update(
            notification);

        await _notificationRepository
            .SaveChangesAsync();

        return MapToDto(notification);
    }

    public async Task CreateBookingCancelledAsync(
        Booking booking)
    {
        var exists =
            await _notificationRepository
                .ExistsAsync(
                    booking.CustomerId,
                    booking.Id,
                    booking.EventId,
                    NotificationType.BookingCancelled);

        if (exists)
        {
            return;
        }

        var notification =
            new Notification
            {
                CustomerId =
                    booking.CustomerId,

                BookingId =
                    booking.Id,

                EventId =
                    booking.EventId,

                Type =
                    NotificationType.BookingCancelled,

                Title =
                    "Booking Cancelled",

                Message =
                    $"Booking '{booking.BookingNumber}' has been cancelled.",

                IsRead =
                    false,

                CreatedAt =
                    DateTime.UtcNow
            };

        await _notificationRepository
            .AddAsync(notification);

        await _notificationRepository
            .SaveChangesAsync();
    }

    public async Task CreateBookingConfirmedAsync(
        Booking booking)
    {
        var exists =
            await _notificationRepository
                .ExistsAsync(
                    booking.CustomerId,
                    booking.Id,
                    booking.EventId,
                    NotificationType.BookingConfirmed);

        if (exists)
        {
            return;
        }

        var notification =
            new Notification
            {
                CustomerId =
                    booking.CustomerId,

                BookingId =
                    booking.Id,

                EventId =
                    booking.EventId,

                Type =
                    NotificationType.BookingConfirmed,

                Title =
                    "Booking Confirmed",

                Message =
                    $"Booking '{booking.BookingNumber}' has been confirmed successfully.",

                IsRead =
                    false,

                CreatedAt =
                    DateTime.UtcNow
            };

        await _notificationRepository
            .AddAsync(notification);

        await _notificationRepository
            .SaveChangesAsync();
    }

    public async Task CreatePaymentCompletedAsync(
        Payment payment)
    {
        var booking =
            payment.Booking;

        var eventId =
            booking?.EventId;

        var exists =
            await _notificationRepository
                .ExistsAsync(
                    payment.CustomerId,
                    payment.BookingId,
                    eventId,
                    NotificationType.PaymentCompleted);

        if (exists)
        {
            return;
        }

        var notification =
            new Notification
            {
                CustomerId =
                    payment.CustomerId,

                BookingId =
                    payment.BookingId,

                EventId =
                    eventId,

                Type =
                    NotificationType.PaymentCompleted,

                Title =
                    "Payment Completed",

                Message =
                    $"Payment of {payment.Amount:0.00} was completed successfully for booking '{booking?.BookingNumber ?? payment.BookingId.ToString()}'.",

                IsRead =
                    false,

                CreatedAt =
                    DateTime.UtcNow
            };

        await _notificationRepository
            .AddAsync(notification);

        await _notificationRepository
            .SaveChangesAsync();
    }

    public async Task<int>
        CreateEventRemindersAsync(
            DateTime fromUtc,
            DateTime toUtc)
    {
        if (toUtc <= fromUtc)
        {
            throw new ValidationException(
                "Reminder end time must be after start time.");
        }

        var bookings =
            await _notificationRepository
                .GetConfirmedBookingsForReminderAsync(
                    fromUtc,
                    toUtc);

        var notifications =
            new List<Notification>();

        foreach (var booking in bookings)
        {
            if (booking.Event == null)
            {
                continue;
            }

            var exists =
                await _notificationRepository
                    .ExistsAsync(
                        booking.CustomerId,
                        booking.Id,
                        booking.EventId,
                        NotificationType.EventReminder);

            if (exists)
            {
                continue;
            }

            notifications.Add(
                new Notification
                {
                    CustomerId =
                        booking.CustomerId,

                    BookingId =
                        booking.Id,

                    EventId =
                        booking.EventId,

                    Type =
                        NotificationType.EventReminder,

                    Title =
                        "Event Reminder",

                    Message =
                        $"Reminder: '{booking.Event.Name}' starts at {booking.Event.StartDateTime:g}.",

                    IsRead =
                        false,

                    CreatedAt =
                        DateTime.UtcNow
                });
        }

        if (notifications.Count == 0)
        {
            return 0;
        }

        await _notificationRepository
            .AddRangeAsync(notifications);

        await _notificationRepository
            .SaveChangesAsync();

        return notifications.Count;
    }

    private static NotificationDto MapToDto(
        Notification notification)
    {
        return new NotificationDto
        {
            Id =
                notification.Id,

            CustomerId =
                notification.CustomerId,

            BookingId =
                notification.BookingId,

            EventId =
                notification.EventId,

            Type =
                notification.Type,

            Title =
                notification.Title,

            Message =
                notification.Message,

            IsRead =
                notification.IsRead,

            CreatedAt =
                notification.CreatedAt
        };
    }
}