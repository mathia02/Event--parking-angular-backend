using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Dashboard;
using EventParking.Models.Enums;

namespace EventParking.Business.Services;

public class DashboardService
    : IDashboardService
{
    private readonly IDashboardRepository
        _dashboardRepository;

    public DashboardService(
        IDashboardRepository dashboardRepository)
    {
        _dashboardRepository =
            dashboardRepository;
    }

    public async Task<AdminDashboardDto>
        GetAdminDashboardAsync()
    {
        var now =
            DateTime.UtcNow;

        var result =
            new AdminDashboardDto
            {
                GeneratedAtUtc =
                    now,

                TotalCustomers =
                    await _dashboardRepository
                        .CountCustomersAsync(),

                TotalVenues =
                    await _dashboardRepository
                        .CountVenuesAsync(),

                TotalCategories =
                    await _dashboardRepository
                        .CountCategoriesAsync(),

                TotalEvents =
                    await _dashboardRepository
                        .CountEventsAsync(),

                UpcomingEvents =
                    await _dashboardRepository
                        .CountUpcomingEventsAsync(
                            now),

                TotalBookings =
                    await _dashboardRepository
                        .CountBookingsAsync(),

                PendingBookings =
                    await _dashboardRepository
                        .CountBookingsByStatusAsync(
                            BookingStatus.Pending),

                ConfirmedBookings =
                    await _dashboardRepository
                        .CountBookingsByStatusAsync(
                            BookingStatus.Confirmed),

                CancelledBookings =
                    await _dashboardRepository
                        .CountBookingsByStatusAsync(
                            BookingStatus.Cancelled),

                ExpiredBookings =
                    await _dashboardRepository
                        .CountBookingsByStatusAsync(
                            BookingStatus.Expired),

                TotalRevenue =
                    await _dashboardRepository
                        .GetTotalRevenueAsync(),

                AvailableSeats =
                    await _dashboardRepository
                        .CountSeatsByStatusAsync(
                            SeatStatus.Available),

                HeldSeats =
                    await _dashboardRepository
                        .CountSeatsByStatusAsync(
                            SeatStatus.Held),

                BookedSeats =
                    await _dashboardRepository
                        .CountSeatsByStatusAsync(
                            SeatStatus.Booked),

                AvailableParkingSlots =
                    await _dashboardRepository
                        .CountParkingSlotsByStatusAsync(
                            ParkingSlotStatus.Available),

                HeldParkingSlots =
                    await _dashboardRepository
                        .CountParkingSlotsByStatusAsync(
                            ParkingSlotStatus.Held),

                BookedParkingSlots =
                    await _dashboardRepository
                        .CountParkingSlotsByStatusAsync(
                            ParkingSlotStatus.Booked),

                UnavailableParkingSlots =
                    await _dashboardRepository
                        .CountParkingSlotsByStatusAsync(
                            ParkingSlotStatus.Unavailable)
            };

        return result;
    }

    public async Task<CustomerDashboardDto>
        GetCustomerDashboardAsync(
            int customerId)
    {
        var now =
            DateTime.UtcNow;

        var result =
            new CustomerDashboardDto
            {
                GeneratedAtUtc =
                    now,

                TotalBookings =
                    await _dashboardRepository
                        .CountCustomerBookingsAsync(
                            customerId),

                PendingBookings =
                    await _dashboardRepository
                        .CountCustomerBookingsByStatusAsync(
                            customerId,
                            BookingStatus.Pending),

                ConfirmedBookings =
                    await _dashboardRepository
                        .CountCustomerBookingsByStatusAsync(
                            customerId,
                            BookingStatus.Confirmed),

                CancelledBookings =
                    await _dashboardRepository
                        .CountCustomerBookingsByStatusAsync(
                            customerId,
                            BookingStatus.Cancelled),

                ExpiredBookings =
                    await _dashboardRepository
                        .CountCustomerBookingsByStatusAsync(
                            customerId,
                            BookingStatus.Expired),

                UpcomingConfirmedBookings =
                    await _dashboardRepository
                        .CountCustomerUpcomingConfirmedBookingsAsync(
                            customerId,
                            now),

                TotalPaid =
                    await _dashboardRepository
                        .GetCustomerTotalPaidAsync(
                            customerId),

                UnreadNotifications =
                    await _dashboardRepository
                        .CountUnreadNotificationsAsync(
                            customerId)
            };

        return result;
    }
}