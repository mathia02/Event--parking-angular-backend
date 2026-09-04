using EventParking.Models.Enums;

namespace EventParking.Models.Entities;

public class Notification
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public int? BookingId { get; set; }

    public int? EventId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }

    // Navigation Properties
    public Customer? Customer { get; set; }

    public Booking? Booking { get; set; }

    public Event? Event { get; set; }
}