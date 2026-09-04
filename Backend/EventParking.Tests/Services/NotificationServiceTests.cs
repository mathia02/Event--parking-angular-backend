using EventParking.Business.Exceptions;
using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Moq;
using Xunit;

namespace EventParking.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository>
        _notificationRepositoryMock;

    private readonly NotificationService
        _notificationService;

    public NotificationServiceTests()
    {
        _notificationRepositoryMock =
            new Mock<INotificationRepository>();

        _notificationService =
            new NotificationService(
                _notificationRepositoryMock.Object);
    }

    // ---------------------------------------------------------
    // GET ALL
    // ---------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_ShouldReturnNotifications()
    {
        // Arrange
        var notification =
            CreateNotification();

        _notificationRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                new List<Notification>
                {
                    notification
                });

        // Act
        var result =
            await _notificationService
                .GetAllAsync();

        // Assert
        Assert.Single(result);

        Assert.Equal(
            notification.Id,
            result[0].Id);

        Assert.Equal(
            notification.CustomerId,
            result[0].CustomerId);

        Assert.Equal(
            notification.Type,
            result[0].Type);

        Assert.Equal(
            notification.Title,
            result[0].Title);
    }

    // ---------------------------------------------------------
    // GET MY NOTIFICATIONS
    // ---------------------------------------------------------

    [Fact]
    public async Task GetMyNotificationsAsync_ShouldReturnCustomerNotifications()
    {
        // Arrange
        var notification =
            CreateNotification();

        _notificationRepositoryMock
            .Setup(x =>
                x.GetByCustomerIdAsync(
                    3,
                    true))
            .ReturnsAsync(
                new List<Notification>
                {
                    notification
                });

        // Act
        var result =
            await _notificationService
                .GetMyNotificationsAsync(
                    3,
                    true);

        // Assert
        Assert.Single(result);

        Assert.Equal(
            3,
            result[0].CustomerId);

        _notificationRepositoryMock.Verify(
            x =>
                x.GetByCustomerIdAsync(
                    3,
                    true),
            Times.Once);
    }

    // ---------------------------------------------------------
    // GET BY ID
    // ---------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_WhenNotificationDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        _notificationRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(999))
            .ReturnsAsync(
                (Notification?)null);

        // Act + Assert
        await Assert.ThrowsAsync<
            NotFoundException>(
            () =>
                _notificationService
                    .GetByIdAsync(
                        999,
                        3,
                        false));
    }

    [Fact]
    public async Task GetByIdAsync_WhenAnotherCustomerRequestsNotification_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var notification =
            CreateNotification();

        _notificationRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(notification);

        // Act + Assert
        await Assert.ThrowsAsync<
            UnauthorizedAccessException>(
            () =>
                _notificationService
                    .GetByIdAsync(
                        1,
                        999,
                        false));
    }

    [Fact]
    public async Task GetByIdAsync_WhenAdministratorRequestsNotification_ShouldReturnNotification()
    {
        // Arrange
        var notification =
            CreateNotification();

        _notificationRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(notification);

        // Act
        var result =
            await _notificationService
                .GetByIdAsync(
                    1,
                    999,
                    true);

        // Assert
        Assert.Equal(
            1,
            result.Id);

        Assert.Equal(
            3,
            result.CustomerId);
    }

    // ---------------------------------------------------------
    // MARK AS READ
    // ---------------------------------------------------------

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        _notificationRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(999))
            .ReturnsAsync(
                (Notification?)null);

        // Act + Assert
        await Assert.ThrowsAsync<
            NotFoundException>(
            () =>
                _notificationService
                    .MarkAsReadAsync(
                        999,
                        3,
                        false));
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenAnotherCustomerRequestsUpdate_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var notification =
            CreateNotification();

        _notificationRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(notification);

        // Act + Assert
        await Assert.ThrowsAsync<
            UnauthorizedAccessException>(
            () =>
                _notificationService
                    .MarkAsReadAsync(
                        1,
                        999,
                        false));

        _notificationRepositoryMock.Verify(
            x =>
                x.Update(
                    It.IsAny<Notification>()),
            Times.Never);
    }

    [Fact]
    public async Task MarkAsReadAsync_WithValidCustomer_ShouldMarkNotificationAsRead()
    {
        // Arrange
        var notification =
            CreateNotification();

        notification.IsRead = false;

        _notificationRepositoryMock
            .Setup(x =>
                x.GetByIdAsync(1))
            .ReturnsAsync(notification);

        _notificationRepositoryMock
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result =
            await _notificationService
                .MarkAsReadAsync(
                    1,
                    3,
                    false);

        // Assert
        Assert.True(
            notification.IsRead);

        Assert.True(
            result.IsRead);

        _notificationRepositoryMock.Verify(
            x =>
                x.Update(notification),
            Times.Once);

        _notificationRepositoryMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Once);
    }

    // ---------------------------------------------------------
    // BOOKING CANCELLED
    // ---------------------------------------------------------

    [Fact]
    public async Task CreateBookingCancelledAsync_WhenNotificationAlreadyExists_ShouldNotCreateDuplicate()
    {
        // Arrange
        var booking =
            CreateBooking();

        _notificationRepositoryMock
            .Setup(x =>
                x.ExistsAsync(
                    booking.CustomerId,
                    booking.Id,
                    booking.EventId,
                    NotificationType.BookingCancelled))
            .ReturnsAsync(true);

        // Act
        await _notificationService
            .CreateBookingCancelledAsync(
                booking);

        // Assert
        _notificationRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<Notification>()),
            Times.Never);

        _notificationRepositoryMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateBookingCancelledAsync_WhenNotificationDoesNotExist_ShouldCreateNotification()
    {
        // Arrange
        var booking =
            CreateBooking();

        Notification? capturedNotification =
            null;

        _notificationRepositoryMock
            .Setup(x =>
                x.ExistsAsync(
                    booking.CustomerId,
                    booking.Id,
                    booking.EventId,
                    NotificationType.BookingCancelled))
            .ReturnsAsync(false);

        _notificationRepositoryMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<Notification>()))
            .Callback<Notification>(
                notification =>
                    capturedNotification =
                        notification)
            .Returns(Task.CompletedTask);

        _notificationRepositoryMock
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _notificationService
            .CreateBookingCancelledAsync(
                booking);

        // Assert
        Assert.NotNull(
            capturedNotification);

        Assert.Equal(
            booking.CustomerId,
            capturedNotification!.CustomerId);

        Assert.Equal(
            booking.Id,
            capturedNotification.BookingId);

        Assert.Equal(
            NotificationType.BookingCancelled,
            capturedNotification.Type);

        Assert.Equal(
            "Booking Cancelled",
            capturedNotification.Title);

        Assert.False(
            capturedNotification.IsRead);

        _notificationRepositoryMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Once);
    }

    // ---------------------------------------------------------
    // BOOKING CONFIRMED
    // ---------------------------------------------------------

    [Fact]
    public async Task CreateBookingConfirmedAsync_WhenNotificationAlreadyExists_ShouldNotCreateDuplicate()
    {
        // Arrange
        var booking =
            CreateBooking();

        _notificationRepositoryMock
            .Setup(x =>
                x.ExistsAsync(
                    booking.CustomerId,
                    booking.Id,
                    booking.EventId,
                    NotificationType.BookingConfirmed))
            .ReturnsAsync(true);

        // Act
        await _notificationService
            .CreateBookingConfirmedAsync(
                booking);

        // Assert
        _notificationRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<Notification>()),
            Times.Never);

        _notificationRepositoryMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateBookingConfirmedAsync_WhenNotificationDoesNotExist_ShouldCreateNotification()
    {
        // Arrange
        var booking =
            CreateBooking();

        Notification? capturedNotification =
            null;

        _notificationRepositoryMock
            .Setup(x =>
                x.ExistsAsync(
                    booking.CustomerId,
                    booking.Id,
                    booking.EventId,
                    NotificationType.BookingConfirmed))
            .ReturnsAsync(false);

        _notificationRepositoryMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<Notification>()))
            .Callback<Notification>(
                notification =>
                    capturedNotification =
                        notification)
            .Returns(Task.CompletedTask);

        _notificationRepositoryMock
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _notificationService
            .CreateBookingConfirmedAsync(
                booking);

        // Assert
        Assert.NotNull(
            capturedNotification);

        Assert.Equal(
            NotificationType.BookingConfirmed,
            capturedNotification!.Type);

        Assert.Equal(
            "Booking Confirmed",
            capturedNotification.Title);

        Assert.Equal(
            booking.Id,
            capturedNotification.BookingId);

        Assert.Equal(
            booking.EventId,
            capturedNotification.EventId);

        Assert.False(
            capturedNotification.IsRead);
    }

    // ---------------------------------------------------------
    // PAYMENT COMPLETED
    // ---------------------------------------------------------

    [Fact]
    public async Task CreatePaymentCompletedAsync_ShouldCreatePaymentCompletedNotification()
    {
        // Arrange
        var booking =
            CreateBooking();

        var payment =
            new Payment
            {
                Id = 1,

                BookingId =
                    booking.Id,

                CustomerId =
                    booking.CustomerId,

                Amount =
                    2500m,

                Status =
                    PaymentStatus.Completed,

                TransactionReference =
                    "PAY-TEST-001",

                CreatedAt =
                    DateTime.UtcNow,

                Booking =
                    booking
            };

        Notification? capturedNotification =
            null;

        _notificationRepositoryMock
            .Setup(x =>
                x.ExistsAsync(
                    payment.CustomerId,
                    payment.BookingId,
                    booking.EventId,
                    NotificationType.PaymentCompleted))
            .ReturnsAsync(false);

        _notificationRepositoryMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<Notification>()))
            .Callback<Notification>(
                notification =>
                    capturedNotification =
                        notification)
            .Returns(Task.CompletedTask);

        _notificationRepositoryMock
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _notificationService
            .CreatePaymentCompletedAsync(
                payment);

        // Assert
        Assert.NotNull(
            capturedNotification);

        Assert.Equal(
            NotificationType.PaymentCompleted,
            capturedNotification!.Type);

        Assert.Equal(
            "Payment Completed",
            capturedNotification.Title);

        Assert.Equal(
            payment.CustomerId,
            capturedNotification.CustomerId);

        Assert.Equal(
            payment.BookingId,
            capturedNotification.BookingId);

        Assert.Equal(
            booking.EventId,
            capturedNotification.EventId);

        Assert.Contains(
            "2500.00",
            capturedNotification.Message);

        Assert.False(
            capturedNotification.IsRead);

        _notificationRepositoryMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Once);
    }

    // ---------------------------------------------------------
    // EVENT REMINDERS
    // ---------------------------------------------------------

    [Fact]
    public async Task CreateEventRemindersAsync_WhenEndTimeIsBeforeStartTime_ShouldThrowValidationException()
    {
        // Arrange
        var fromUtc =
            DateTime.UtcNow;

        var toUtc =
            fromUtc.AddHours(-1);

        // Act + Assert
        await Assert.ThrowsAsync<
            ValidationException>(
            () =>
                _notificationService
                    .CreateEventRemindersAsync(
                        fromUtc,
                        toUtc));
    }

    [Fact]
    public async Task CreateEventRemindersAsync_WhenNoConfirmedBookings_ShouldReturnZero()
    {
        // Arrange
        var fromUtc =
            DateTime.UtcNow;

        var toUtc =
            fromUtc.AddHours(24);

        _notificationRepositoryMock
            .Setup(x =>
                x.GetConfirmedBookingsForReminderAsync(
                    fromUtc,
                    toUtc))
            .ReturnsAsync(
                new List<Booking>());

        // Act
        var result =
            await _notificationService
                .CreateEventRemindersAsync(
                    fromUtc,
                    toUtc);

        // Assert
        Assert.Equal(
            0,
            result);

        _notificationRepositoryMock.Verify(
            x =>
                x.AddRangeAsync(
                    It.IsAny<
                        IEnumerable<Notification>>()),
            Times.Never);

        _notificationRepositoryMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateEventRemindersAsync_ShouldCreateReminderAndSkipDuplicate()
    {
        // Arrange
        var fromUtc =
            DateTime.UtcNow;

        var toUtc =
            fromUtc.AddHours(24);

        var booking1 =
            CreateBooking(
                id: 10,
                customerId: 3);

        var booking2 =
            CreateBooking(
                id: 11,
                customerId: 4);

        booking1.Event =
            CreateEvent();

        booking2.Event =
            CreateEvent();

        _notificationRepositoryMock
            .Setup(x =>
                x.GetConfirmedBookingsForReminderAsync(
                    fromUtc,
                    toUtc))
            .ReturnsAsync(
                new List<Booking>
                {
                    booking1,
                    booking2
                });

        _notificationRepositoryMock
            .Setup(x =>
                x.ExistsAsync(
                    booking1.CustomerId,
                    booking1.Id,
                    booking1.EventId,
                    NotificationType.EventReminder))
            .ReturnsAsync(false);

        _notificationRepositoryMock
            .Setup(x =>
                x.ExistsAsync(
                    booking2.CustomerId,
                    booking2.Id,
                    booking2.EventId,
                    NotificationType.EventReminder))
            .ReturnsAsync(true);

        List<Notification> capturedNotifications =
            new();

        _notificationRepositoryMock
            .Setup(x =>
                x.AddRangeAsync(
                    It.IsAny<
                        IEnumerable<Notification>>()))
            .Callback<
                IEnumerable<Notification>>(
                notifications =>
                    capturedNotifications =
                        notifications.ToList())
            .Returns(Task.CompletedTask);

        _notificationRepositoryMock
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result =
            await _notificationService
                .CreateEventRemindersAsync(
                    fromUtc,
                    toUtc);

        // Assert
        Assert.Equal(
            1,
            result);

        Assert.Single(
            capturedNotifications);

        var reminder =
            capturedNotifications[0];

        Assert.Equal(
            booking1.CustomerId,
            reminder.CustomerId);

        Assert.Equal(
            booking1.Id,
            reminder.BookingId);

        Assert.Equal(
            booking1.EventId,
            reminder.EventId);

        Assert.Equal(
            NotificationType.EventReminder,
            reminder.Type);

        Assert.Equal(
            "Event Reminder",
            reminder.Title);

        Assert.False(
            reminder.IsRead);

        _notificationRepositoryMock.Verify(
            x =>
                x.AddRangeAsync(
                    It.IsAny<
                        IEnumerable<Notification>>()),
            Times.Once);

        _notificationRepositoryMock.Verify(
            x =>
                x.SaveChangesAsync(),
            Times.Once);
    }

    // ---------------------------------------------------------
    // HELPERS
    // ---------------------------------------------------------

    private static Notification
        CreateNotification()
    {
        return new Notification
        {
            Id = 1,

            CustomerId = 3,

            BookingId = 1,

            EventId = 1,

            Type =
                NotificationType.BookingConfirmed,

            Title =
                "Booking Confirmed",

            Message =
                "Your booking has been confirmed.",

            IsRead =
                false,

            CreatedAt =
                DateTime.UtcNow
        };
    }

    private static Booking CreateBooking(
        int id = 1,
        int customerId = 3)
    {
        return new Booking
        {
            Id = id,

            BookingNumber =
                $"BKG-NOTIFICATION-{id}",

            CustomerId =
                customerId,

            EventId =
                1,

            Status =
                BookingStatus.Confirmed,

            CreatedAt =
                DateTime.UtcNow,

            ConfirmedAt =
                DateTime.UtcNow
        };
    }

    private static EventParking.Models.Entities.Event
        CreateEvent()
    {
        return new EventParking.Models.Entities.Event
        {
            Id = 1,

            Name =
                "Tech Conference 2026",

            VenueId =
                1,

            CategoryId =
                1,

            StartDateTime =
                DateTime.UtcNow.AddHours(12),

            EndDateTime =
                DateTime.UtcNow.AddHours(14),

            TicketPrice =
                2500m,

            ParkingFee =
                500m,

            Capacity =
                300,

            CreatedAt =
                DateTime.UtcNow
        };
    }
}