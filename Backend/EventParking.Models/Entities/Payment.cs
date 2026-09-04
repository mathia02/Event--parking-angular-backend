using EventParking.Models.Enums;

namespace EventParking.Models.Entities;

public class Payment
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public int CustomerId { get; set; }

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }
        = PaymentStatus.Pending;

    public string? TransactionReference { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    // Navigation Properties
    public Booking? Booking { get; set; }

    public Customer? Customer { get; set; }
}