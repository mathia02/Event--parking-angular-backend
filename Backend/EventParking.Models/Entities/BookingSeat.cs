namespace EventParking.Models.Entities;

public class BookingSeat
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public int SeatId { get; set; }

    // Seat price at the exact time of booking
    public decimal PriceAtBooking { get; set; }

    // Active reservation or historical released reservation
    public bool IsActive { get; set; } = true;

    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReleasedAt { get; set; }

    // Navigation Properties
    public Booking? Booking { get; set; }

    public Seat? Seat { get; set; }
}