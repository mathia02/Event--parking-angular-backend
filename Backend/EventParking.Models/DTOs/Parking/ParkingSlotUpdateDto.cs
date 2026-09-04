using System.ComponentModel.DataAnnotations;
using EventParking.Models.Enums;

namespace EventParking.Models.DTOs.Parking;

public class ParkingSlotUpdateDto
{
    [Required]
    [MaxLength(50)]
    public string SlotNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Zone { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Fee { get; set; }

    public ParkingSlotStatus Status { get; set; } =
        ParkingSlotStatus.Available;
}