using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class SeatRepository : ISeatRepository
{
    private readonly ApplicationDbContext _context;

    public SeatRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Seat>> GetByEventIdAsync(
        int eventId)
    {
        return await _context.Seats
            .AsNoTracking()
            .Where(s => s.EventId == eventId)
            .OrderBy(s => s.RowLabel)
            .ThenBy(s => s.ColumnNumber)
            .ToListAsync();
    }

    public async Task<List<Seat>>
        GetAvailableByEventIdAsync(int eventId)
    {
        return await _context.Seats
            .AsNoTracking()
            .Where(s =>
                s.EventId == eventId &&
                s.Status == SeatStatus.Available)
            .OrderBy(s => s.RowLabel)
            .ThenBy(s => s.ColumnNumber)
            .ToListAsync();
    }

    public async Task<Seat?> GetByIdAsync(int id)
    {
        return await _context.Seats
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<int> GetSeatCountByEventAsync(
        int eventId)
    {
        return await _context.Seats
            .AsNoTracking()
            .CountAsync(s => s.EventId == eventId);
    }

    public async Task<bool> SeatNumberExistsAsync(
        int eventId,
        string seatNumber,
        int? excludeSeatId = null)
    {
        var normalizedSeatNumber =
            seatNumber.Trim();

        var query = _context.Seats
            .AsNoTracking()
            .Where(s =>
                s.EventId == eventId &&
                s.SeatNumber == normalizedSeatNumber);

        if (excludeSeatId.HasValue)
        {
            query = query.Where(s =>
                s.Id != excludeSeatId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> HasActiveBookingAsync(
        int seatId)
    {
        return await _context.BookingSeats
            .AsNoTracking()
            .AnyAsync(bs =>
                bs.SeatId == seatId &&
                bs.IsActive &&
                bs.Booking != null &&
                (
                    bs.Booking.Status ==
                        BookingStatus.Pending ||
                    bs.Booking.Status ==
                        BookingStatus.Confirmed
                ));
    }

    public async Task AddAsync(Seat seat)
    {
        await _context.Seats.AddAsync(seat);
    }

    public async Task AddRangeAsync(
        IEnumerable<Seat> seats)
    {
        await _context.Seats.AddRangeAsync(seats);
    }

    public void Update(Seat seat)
    {
        _context.Seats.Update(seat);
    }

    public void Delete(Seat seat)
    {
        _context.Seats.Remove(seat);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}