using System.ComponentModel.DataAnnotations;

namespace EventParking.Models.DTOs.Event;

public class EventCreateDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int VenueId { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Required]
    public DateTime StartDateTime { get; set; }

    [Required]
    public DateTime EndDateTime { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TicketPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ParkingFee { get; set; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }
}