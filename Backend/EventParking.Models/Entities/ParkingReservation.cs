namespace EventParking.Models.Entities;

public class ParkingReservation
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public int ParkingSlotId { get; set; }

    // Parking fee at reservation time
    public decimal ReservedFee { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReleasedAt { get; set; }

    // Navigation Properties
    public Booking? Booking { get; set; }

    public ParkingSlot? ParkingSlot { get; set; }
}