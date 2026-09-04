using EventParking.Models.Enums;

namespace EventParking.Models.DTOs.Payment;

public class PaymentDto
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public string BookingNumber { get; set; }
        = string.Empty;

    public int CustomerId { get; set; }

    public string CustomerName { get; set; }
        = string.Empty;

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }

    public string TransactionReference { get; set; }
        = string.Empty;

    public DateTime CreatedAt { get; set; }
}