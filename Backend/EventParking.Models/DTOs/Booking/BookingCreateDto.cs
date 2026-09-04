using System.ComponentModel.DataAnnotations;

namespace EventParking.Models.DTOs.Booking;

public class BookingCreateDto
{
    [Required]
    public int EventId { get; set; }

    [Required]
    [MinLength(1)]
    public List<int> SeatIds { get; set; } = new();

    public int? ParkingSlotId { get; set; }
}