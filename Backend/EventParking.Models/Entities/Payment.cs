using EventParking.Models.Enums;

namespace EventParking.Models.Entities;

public class Payment
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public int CustomerId { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
        = PaymentMethod.NotSpecified;

    public PaymentStatus Status { get; set; }
        = PaymentStatus.Pending;

    public string TransactionReference { get; set; }
        = string.Empty;

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    public Booking? Booking { get; set; }

    public Customer? Customer { get; set; }
}