using EventParking.Models.Enums;

namespace EventParking.Models.Entities;

public class Booking
{
    public int Id { get; set; }

    public string BookingNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public int EventId { get; set; }

    public BookingStatus Status { get; set; }
        = BookingStatus.Pending;

    // Pending booking hold expiry
    public DateTime? HoldExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    // Navigation Properties
    public Customer? Customer { get; set; }

    public Event? Event { get; set; }

    public ICollection<BookingSeat> BookingSeats { get; set; }
        = new List<BookingSeat>();

    public ParkingReservation? ParkingReservation { get; set; }

    public Payment? Payment { get; set; }
}