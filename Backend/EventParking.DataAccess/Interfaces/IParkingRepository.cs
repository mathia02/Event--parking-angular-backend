using EventParking.Models.Entities;

namespace EventParking.DataAccess.Interfaces;

public interface IParkingRepository
{
    Task<List<ParkingSlot>> GetByEventIdAsync(int eventId);

    Task<List<ParkingSlot>> GetAvailableByEventIdAsync(int eventId);

    Task<ParkingSlot?> GetByIdAsync(int id);

    Task<bool> SlotNumberExistsAsync(
        int eventId,
        string slotNumber,
        int? excludeParkingSlotId = null);

    Task<bool> HasActiveReservationAsync(
        int parkingSlotId);

    Task AddAsync(
        ParkingSlot parkingSlot);
    
    void Update(
        ParkingSlot parkingSlot);

    void Delete(
        ParkingSlot parkingSlot);

    Task<int> SaveChangesAsync();
}