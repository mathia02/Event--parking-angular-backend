using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class ParkingRepository : IParkingRepository
{
    private readonly ApplicationDbContext _context;

    public ParkingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ParkingSlot>> GetByEventIdAsync(int eventId)
    {
        return await _context.ParkingSlots
            .AsNoTracking()
            .Where(x => x.EventId == eventId)
            .OrderBy(x => x.Zone)
            .ThenBy(x => x.SlotNumber)
            .ToListAsync();
    }

    public async Task<List<ParkingSlot>> GetAvailableByEventIdAsync(
        int eventId)
    {
        return await _context.ParkingSlots
            .AsNoTracking()
            .Where(x =>
                x.EventId == eventId &&
                x.Status == ParkingSlotStatus.Available)
            .OrderBy(x => x.Zone)
            .ThenBy(x => x.SlotNumber)
            .ToListAsync();
    }

    public async Task<ParkingSlot?> GetByIdAsync(int id)
    {
        return await _context.ParkingSlots
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> SlotNumberExistsAsync(
        int eventId,
        string slotNumber,
        int? excludeParkingSlotId = null)
    {
        var query = _context.ParkingSlots
            .Where(x =>
                x.EventId == eventId &&
                x.SlotNumber == slotNumber);

        if (excludeParkingSlotId.HasValue)
        {
            query = query.Where(
                x => x.Id != excludeParkingSlotId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> HasActiveReservationAsync(
        int parkingSlotId)
    {
        return await _context.ParkingReservations
            .AnyAsync(x =>
                x.ParkingSlotId == parkingSlotId &&
                x.IsActive &&
                x.Booking != null &&
                (
                    x.Booking.Status == BookingStatus.Pending ||
                    x.Booking.Status == BookingStatus.Confirmed
                ));
    }

    public async Task AddAsync(ParkingSlot parkingSlot)
    {
        await _context.ParkingSlots.AddAsync(parkingSlot);
    }

    public void Update(ParkingSlot parkingSlot)
    {
        _context.ParkingSlots.Update(parkingSlot);
    }

    public void Delete(ParkingSlot parkingSlot)
    {
        _context.ParkingSlots.Remove(parkingSlot);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}