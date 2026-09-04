using EventParking.Models.Enums;

namespace EventParking.DataAccess.Interfaces;

public interface IDashboardRepository
{
    Task<int> CountCustomersAsync();

    Task<int> CountVenuesAsync();

    Task<int> CountCategoriesAsync();

    Task<int> CountEventsAsync();

    Task<int> CountUpcomingEventsAsync(
        DateTime utcNow);

    Task<int> CountBookingsAsync();

    Task<int> CountBookingsByStatusAsync(
        BookingStatus status);

    Task<decimal> GetTotalRevenueAsync();

    Task<int> CountSeatsByStatusAsync(
        SeatStatus status);

    Task<int> CountParkingSlotsByStatusAsync(
        ParkingSlotStatus status);

    Task<int> CountCustomerBookingsAsync(
        int customerId);

    Task<int> CountCustomerBookingsByStatusAsync(
        int customerId,
        BookingStatus status);

    Task<int>
        CountCustomerUpcomingConfirmedBookingsAsync(
            int customerId,
            DateTime utcNow);

    Task<decimal> GetCustomerTotalPaidAsync(
        int customerId);

    Task<int> CountUnreadNotificationsAsync(
        int customerId);
}