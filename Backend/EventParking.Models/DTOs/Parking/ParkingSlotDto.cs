using EventParking.Models.Enums;

namespace EventParking.Models.DTOs.Parking;

public class ParkingSlotDto
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string SlotNumber { get; set; } = string.Empty;

    public string Zone { get; set; } = string.Empty;

    public decimal Fee { get; set; }

    public ParkingSlotStatus Status { get; set; }
}