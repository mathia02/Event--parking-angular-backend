using EventParking.Models.Enums;

namespace EventParking.Models.DTOs.Booking;

public class BookingDto
{
    public int Id { get; set; }

    public string BookingNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public BookingStatus Status { get; set; }

    public DateTime? HoldExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
}