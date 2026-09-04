using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Enums;
using Moq;
using Xunit;

namespace EventParking.Tests.Services;

public class DashboardServiceTests
{
    private readonly Mock<IDashboardRepository>
        _dashboardRepositoryMock;

    private readonly DashboardService
        _dashboardService;

    public DashboardServiceTests()
    {
        _dashboardRepositoryMock =
            new Mock<IDashboardRepository>();

        _dashboardService =
            new DashboardService(
                _dashboardRepositoryMock.Object);
    }

    // ---------------------------------------------------------
    // ADMIN DASHBOARD
    // ---------------------------------------------------------

    [Fact]
    public async Task GetAdminDashboardAsync_ShouldReturnCorrectDashboardSummary()
    {
        // Arrange
        _dashboardRepositoryMock
            .Setup(x => x.CountCustomersAsync())
            .ReturnsAsync(5);

        _dashboardRepositoryMock
            .Setup(x => x.CountVenuesAsync())
            .ReturnsAsync(2);

        _dashboardRepositoryMock
            .Setup(x => x.CountCategoriesAsync())
            .ReturnsAsync(3);

        _dashboardRepositoryMock
            .Setup(x => x.CountEventsAsync())
            .ReturnsAsync(4);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountUpcomingEventsAsync(
                    It.IsAny<DateTime>()))
            .ReturnsAsync(3);

        _dashboardRepositoryMock
            .Setup(x => x.CountBookingsAsync())
            .ReturnsAsync(10);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountBookingsByStatusAsync(
                    BookingStatus.Pending))
            .ReturnsAsync(2);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountBookingsByStatusAsync(
                    BookingStatus.Confirmed))
            .ReturnsAsync(4);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountBookingsByStatusAsync(
                    BookingStatus.Cancelled))
            .ReturnsAsync(2);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountBookingsByStatusAsync(
                    BookingStatus.Expired))
            .ReturnsAsync(2);

        _dashboardRepositoryMock
            .Setup(x => x.GetTotalRevenueAsync())
            .ReturnsAsync(15000m);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountSeatsByStatusAsync(
                    SeatStatus.Available))
            .ReturnsAsync(280);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountSeatsByStatusAsync(
                    SeatStatus.Held))
            .ReturnsAsync(5);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountSeatsByStatusAsync(
                    SeatStatus.Booked))
            .ReturnsAsync(15);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountParkingSlotsByStatusAsync(
                    ParkingSlotStatus.Available))
            .ReturnsAsync(10);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountParkingSlotsByStatusAsync(
                    ParkingSlotStatus.Held))
            .ReturnsAsync(2);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountParkingSlotsByStatusAsync(
                    ParkingSlotStatus.Booked))
            .ReturnsAsync(4);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountParkingSlotsByStatusAsync(
                    ParkingSlotStatus.Unavailable))
            .ReturnsAsync(1);

        // Act
        var result =
            await _dashboardService
                .GetAdminDashboardAsync();

        // Assert
        Assert.Equal(5, result.TotalCustomers);

        Assert.Equal(2, result.TotalVenues);

        Assert.Equal(3, result.TotalCategories);

        Assert.Equal(4, result.TotalEvents);

        Assert.Equal(3, result.UpcomingEvents);

        Assert.Equal(10, result.TotalBookings);

        Assert.Equal(2, result.PendingBookings);

        Assert.Equal(4, result.ConfirmedBookings);

        Assert.Equal(2, result.CancelledBookings);

        Assert.Equal(2, result.ExpiredBookings);

        Assert.Equal(
            15000m,
            result.TotalRevenue);

        Assert.Equal(
            280,
            result.AvailableSeats);

        Assert.Equal(
            5,
            result.HeldSeats);

        Assert.Equal(
            15,
            result.BookedSeats);

        Assert.Equal(
            10,
            result.AvailableParkingSlots);

        Assert.Equal(
            2,
            result.HeldParkingSlots);

        Assert.Equal(
            4,
            result.BookedParkingSlots);

        Assert.Equal(
            1,
            result.UnavailableParkingSlots);

        Assert.True(
            result.GeneratedAtUtc <=
            DateTime.UtcNow);
    }

    [Fact]
    public async Task GetAdminDashboardAsync_ShouldRequestAllBookingAndResourceStatuses()
    {
        // Arrange
        SetupAdminDefaults();

        // Act
        await _dashboardService
            .GetAdminDashboardAsync();

        // Assert
        _dashboardRepositoryMock.Verify(
            x =>
                x.CountBookingsByStatusAsync(
                    BookingStatus.Pending),
            Times.Once);

        _dashboardRepositoryMock.Verify(
            x =>
                x.CountBookingsByStatusAsync(
                    BookingStatus.Confirmed),
            Times.Once);

        _dashboardRepositoryMock.Verify(
            x =>
                x.CountBookingsByStatusAsync(
                    BookingStatus.Cancelled),
            Times.Once);

        _dashboardRepositoryMock.Verify(
            x =>
                x.CountBookingsByStatusAsync(
                    BookingStatus.Expired),
            Times.Once);

        _dashboardRepositoryMock.Verify(
            x =>
                x.CountSeatsByStatusAsync(
                    SeatStatus.Available),
            Times.Once);

        _dashboardRepositoryMock.Verify(
            x =>
                x.CountSeatsByStatusAsync(
                    SeatStatus.Held),
            Times.Once);

        _dashboardRepositoryMock.Verify(
            x =>
                x.CountSeatsByStatusAsync(
                    SeatStatus.Booked),
            Times.Once);

        _dashboardRepositoryMock.Verify(
            x =>
                x.CountParkingSlotsByStatusAsync(
                    ParkingSlotStatus.Available),
            Times.Once);

        _dashboardRepositoryMock.Verify(
            x =>
                x.CountParkingSlotsByStatusAsync(
                    ParkingSlotStatus.Held),
            Times.Once);

        _dashboardRepositoryMock.Verify(
            x =>
                x.CountParkingSlotsByStatusAsync(
                    ParkingSlotStatus.Booked),
            Times.Once);

        _dashboardRepositoryMock.Verify(
            x =>
                x.CountParkingSlotsByStatusAsync(
                    ParkingSlotStatus.Unavailable),
            Times.Once);
    }

    // ---------------------------------------------------------
    // CUSTOMER DASHBOARD
    // ---------------------------------------------------------

    [Fact]
    public async Task GetCustomerDashboardAsync_ShouldReturnCorrectDashboardSummary()
    {
        // Arrange
        const int customerId = 3;

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountCustomerBookingsAsync(
                    customerId))
            .ReturnsAsync(6);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountCustomerBookingsByStatusAsync(
                    customerId,
                    BookingStatus.Pending))
            .ReturnsAsync(1);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountCustomerBookingsByStatusAsync(
                    customerId,
                    BookingStatus.Confirmed))
            .ReturnsAsync(2);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountCustomerBookingsByStatusAsync(
                    customerId,
                    BookingStatus.Cancelled))
            .ReturnsAsync(2);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountCustomerBookingsByStatusAsync(
                    customerId,
                    BookingStatus.Expired))
            .ReturnsAsync(1);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountCustomerUpcomingConfirmedBookingsAsync(
                    customerId,
                    It.IsAny<DateTime>()))
            .ReturnsAsync(1);

        _dashboardRepositoryMock
            .Setup(x =>
                x.GetCustomerTotalPaidAsync(
                    customerId))
            .ReturnsAsync(6500m);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountUnreadNotificationsAsync(
                    customerId))
            .ReturnsAsync(3);

        // Act
        var result =
            await _dashboardService
                .GetCustomerDashboardAsync(
                    customerId);

        // Assert
        Assert.Equal(
            6,
            result.TotalBookings);

        Assert.Equal(
            1,
            result.PendingBookings);

        Assert.Equal(
            2,
            result.ConfirmedBookings);

        Assert.Equal(
            2,
            result.CancelledBookings);

        Assert.Equal(
            1,
            result.ExpiredBookings);

        Assert.Equal(
            1,
            result.UpcomingConfirmedBookings);

        Assert.Equal(
            6500m,
            result.TotalPaid);

        Assert.Equal(
            3,
            result.UnreadNotifications);

        Assert.True(
            result.GeneratedAtUtc <=
            DateTime.UtcNow);
    }

    [Fact]
    public async Task GetCustomerDashboardAsync_ShouldUseRequestedCustomerIdForAllQueries()
    {
        // Arrange
        const int customerId = 25;

        SetupCustomerDefaults(
            customerId);

        // Act
        await _dashboardService
            .GetCustomerDashboardAsync(
                customerId);

        // Assert
        _dashboardRepositoryMock.Verify(
            x =>
                x.CountCustomerBookingsAsync(
                    customerId),
            Times.Once);

        _dashboardRepositoryMock.Verify(
            x =>
                x.CountCustomerBookingsByStatusAsync(
                    customerId,
                    BookingStatus.Pending),
            Times.Once);

        _dashboardRepositoryMock.Verify(
            x =>
                x.CountCustomerBookingsByStatusAsync(
                    customerId,
                    BookingStatus.Confirmed),
            Times.Once);

        _dashboardRepositoryMock.Verify(
            x =>
                x.CountCustomerBookingsByStatusAsync(
                    customerId,
                    BookingStatus.Cancelled),
            Times.Once);

        _dashboardRepositoryMock.Verify(
            x =>
                x.CountCustomerBookingsByStatusAsync(
                    customerId,
                    BookingStatus.Expired),
            Times.Once);

        _dashboardRepositoryMock.Verify(
            x =>
                x.CountCustomerUpcomingConfirmedBookingsAsync(
                    customerId,
                    It.IsAny<DateTime>()),
            Times.Once);

        _dashboardRepositoryMock.Verify(
            x =>
                x.GetCustomerTotalPaidAsync(
                    customerId),
            Times.Once);

        _dashboardRepositoryMock.Verify(
            x =>
                x.CountUnreadNotificationsAsync(
                    customerId),
            Times.Once);
    }

    // ---------------------------------------------------------
    // HELPERS
    // ---------------------------------------------------------

    private void SetupAdminDefaults()
    {
        _dashboardRepositoryMock
            .Setup(x => x.CountCustomersAsync())
            .ReturnsAsync(0);

        _dashboardRepositoryMock
            .Setup(x => x.CountVenuesAsync())
            .ReturnsAsync(0);

        _dashboardRepositoryMock
            .Setup(x => x.CountCategoriesAsync())
            .ReturnsAsync(0);

        _dashboardRepositoryMock
            .Setup(x => x.CountEventsAsync())
            .ReturnsAsync(0);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountUpcomingEventsAsync(
                    It.IsAny<DateTime>()))
            .ReturnsAsync(0);

        _dashboardRepositoryMock
            .Setup(x => x.CountBookingsAsync())
            .ReturnsAsync(0);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountBookingsByStatusAsync(
                    It.IsAny<BookingStatus>()))
            .ReturnsAsync(0);

        _dashboardRepositoryMock
            .Setup(x => x.GetTotalRevenueAsync())
            .ReturnsAsync(0m);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountSeatsByStatusAsync(
                    It.IsAny<SeatStatus>()))
            .ReturnsAsync(0);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountParkingSlotsByStatusAsync(
                    It.IsAny<ParkingSlotStatus>()))
            .ReturnsAsync(0);
    }

    private void SetupCustomerDefaults(
        int customerId)
    {
        _dashboardRepositoryMock
            .Setup(x =>
                x.CountCustomerBookingsAsync(
                    customerId))
            .ReturnsAsync(0);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountCustomerBookingsByStatusAsync(
                    customerId,
                    It.IsAny<BookingStatus>()))
            .ReturnsAsync(0);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountCustomerUpcomingConfirmedBookingsAsync(
                    customerId,
                    It.IsAny<DateTime>()))
            .ReturnsAsync(0);

        _dashboardRepositoryMock
            .Setup(x =>
                x.GetCustomerTotalPaidAsync(
                    customerId))
            .ReturnsAsync(0m);

        _dashboardRepositoryMock
            .Setup(x =>
                x.CountUnreadNotificationsAsync(
                    customerId))
            .ReturnsAsync(0);
    }
}