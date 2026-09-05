using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _context;

    public BookingRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Booking>> GetAllAsync()
    {
        return await _context.Bookings
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Event)
            .Include(x => x.BookingSeats)
                .ThenInclude(x => x.Seat)
            .Include(x => x.ParkingReservation)
                .ThenInclude(x => x!.ParkingSlot)
            .Include(x => x.Payment)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Booking>>
        GetByCustomerIdAsync(
            int customerId)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(x =>
                x.CustomerId == customerId)
            .Include(x => x.Customer)
            .Include(x => x.Event)
            .Include(x => x.BookingSeats)
                .ThenInclude(x => x.Seat)
            .Include(x => x.ParkingReservation)
                .ThenInclude(x => x!.ParkingSlot)
            .Include(x => x.Payment)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<Booking?>
        GetByIdAsync(int id)
    {
        return await _context.Bookings
            .Include(x => x.Customer)
            .Include(x => x.Event)
            .Include(x => x.BookingSeats)
                .ThenInclude(x => x.Seat)
            .Include(x => x.ParkingReservation)
                .ThenInclude(x => x!.ParkingSlot)

            // IMPORTANT:
            // Required for cancellation/payment rule.
            .Include(x => x.Payment)

            .FirstOrDefaultAsync(
                x => x.Id == id);
    }

    public async Task<List<Booking>>
        GetExpiredPendingBookingsAsync(
            DateTime utcNow)
    {
        return await _context.Bookings
            .Where(x =>
                x.Status ==
                    BookingStatus.Pending &&
                x.HoldExpiresAt.HasValue &&
                x.HoldExpiresAt.Value <= utcNow)
            .Include(x => x.BookingSeats)
                .ThenInclude(x => x.Seat)
            .Include(x => x.ParkingReservation)
                .ThenInclude(x => x!.ParkingSlot)
            .ToListAsync();
    }

    public async Task<bool>
        BookingNumberExistsAsync(
            string bookingNumber)
    {
        return await _context.Bookings
            .AnyAsync(x =>
                x.BookingNumber ==
                bookingNumber);
    }

    public async Task AddAsync(
        Booking booking)
    {
        await _context.Bookings
            .AddAsync(booking);
    }

    public async Task AddBookingSeatsAsync(
        IEnumerable<BookingSeat>
            bookingSeats)
    {
        await _context.BookingSeats
            .AddRangeAsync(
                bookingSeats);
    }

    public async Task
        AddParkingReservationAsync(
            ParkingReservation
                parkingReservation)
    {
        await _context.ParkingReservations
            .AddAsync(
                parkingReservation);
    }

    public async Task<int>
        SaveChangesAsync()
    {
        return await _context
            .SaveChangesAsync();
    }

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