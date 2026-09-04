using EventParking.Models.Enums;

namespace EventParking.Models.Entities;

public class ParkingSlot
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string SlotNumber { get; set; } = string.Empty;

    public string? Zone { get; set; }

    public decimal? Fee { get; set; }

    public ParkingSlotStatus Status { get; set; }
        = ParkingSlotStatus.Available;

    // Used later for concurrency protection
    public byte[] RowVersion { get; set; }
        = Array.Empty<byte>();

    // Navigation Properties
    public Event? Event { get; set; }

    public ICollection<ParkingReservation> ParkingReservations { get; set; }
        = new List<ParkingReservation>();
}