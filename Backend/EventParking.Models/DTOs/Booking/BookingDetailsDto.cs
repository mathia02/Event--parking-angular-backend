using EventParking.Models.Enums;

namespace EventParking.Models.DTOs.Booking;

public class BookingDetailsDto
{
    public int Id { get; set; }

    public string BookingNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public BookingStatus Status { get; set; }

    public DateTime? HoldExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public List<BookingSeatItemDto> Seats { get; set; } = new();

    public BookingParkingDto? Parking { get; set; }

    public decimal TotalAmount { get; set; }
}

public class BookingSeatItemDto
{
    public int SeatId { get; set; }

    public string SeatNumber { get; set; } = string.Empty;

    public string SeatType { get; set; } = string.Empty;

    public decimal PriceAtBooking { get; set; }
}
 
public class BookingParkingDto
{
    public int ParkingSlotId { get; set; }

    public string SlotNumber { get; set; } = string.Empty;

    public string Zone { get; set; } = string.Empty;

    public decimal ReservedFee { get; set; }
}