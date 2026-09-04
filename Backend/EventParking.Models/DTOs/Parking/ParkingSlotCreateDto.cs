using System.ComponentModel.DataAnnotations;

namespace EventParking.Models.DTOs.Parking;

public class ParkingSlotCreateDto
{
    [Required]
    [MaxLength(50)]
    public string SlotNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Zone { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Fee { get; set; }
}