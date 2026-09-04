using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class DashboardRepository
    : IDashboardRepository
{
    private readonly ApplicationDbContext _context;

    public DashboardRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> CountCustomersAsync()
    {
        return await _context.Customers
            .CountAsync();
    }

    public async Task<int> CountVenuesAsync()
    {
        return await _context.Venues
            .CountAsync();
    }

    public async Task<int> CountCategoriesAsync()
    {
        return await _context.EventCategories
            .CountAsync();
    }

    public async Task<int> CountEventsAsync()
    {
        return await _context.Events
            .CountAsync();
    }

    public async Task<int> CountUpcomingEventsAsync(
        DateTime utcNow)
    {
        return await _context.Events
            .CountAsync(x =>
                x.StartDateTime > utcNow);
    }

    public async Task<int> CountBookingsAsync()
    {
        return await _context.Bookings
            .CountAsync();
    }

    public async Task<int> CountBookingsByStatusAsync(
        BookingStatus status)
    {
        return await _context.Bookings
            .CountAsync(x =>
                x.Status == status);
    }

    public async Task<decimal>
        GetTotalRevenueAsync()
    {
        var total =
            await _context.Payments
                .Where(x =>
                    x.Status ==
                    PaymentStatus.Completed)
                .SumAsync(x =>
                    (decimal?)x.Amount);

        return total ?? 0m;
    }

    public async Task<int> CountSeatsByStatusAsync(
        SeatStatus status)
    {
        return await _context.Seats
            .CountAsync(x =>
                x.Status == status);
    }

    public async Task<int>
        CountParkingSlotsByStatusAsync(
            ParkingSlotStatus status)
    {
        return await _context.ParkingSlots
            .CountAsync(x =>
                x.Status == status);
    }

    public async Task<int>
        CountCustomerBookingsAsync(
            int customerId)
    {
        return await _context.Bookings
            .CountAsync(x =>
                x.CustomerId == customerId);
    }

    public async Task<int>
        CountCustomerBookingsByStatusAsync(
            int customerId,
            BookingStatus status)
    {
        return await _context.Bookings
            .CountAsync(x =>
                x.CustomerId == customerId &&
                x.Status == status);
    }

    public async Task<int>
        CountCustomerUpcomingConfirmedBookingsAsync(
            int customerId,
            DateTime utcNow)
    {
        return await _context.Bookings
            .CountAsync(x =>
                x.CustomerId == customerId &&
                x.Status ==
                    BookingStatus.Confirmed &&
                x.Event != null &&
                x.Event.StartDateTime >
                    utcNow);
    }

    public async Task<decimal>
        GetCustomerTotalPaidAsync(
            int customerId)
    {
        var total =
            await _context.Payments
                .Where(x =>
                    x.CustomerId ==
                        customerId &&
                    x.Status ==
                        PaymentStatus.Completed)
                .SumAsync(x =>
                    (decimal?)x.Amount);

        return total ?? 0m;
    }

    public async Task<int>
        CountUnreadNotificationsAsync(
            int customerId)
    {
        return await _context.Notifications
            .CountAsync(x =>
                x.CustomerId ==
                    customerId &&
                !x.IsRead);
    }
}