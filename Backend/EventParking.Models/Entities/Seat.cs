using EventParking.Models.Enums;

namespace EventParking.Models.Entities;

public class Seat
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string SeatNumber { get; set; } = string.Empty;

    public string RowLabel { get; set; } = string.Empty;

    public int ColumnNumber { get; set; }

    public string? SeatType { get; set; }

    // Optional custom price for VIP/Premium seat
    public decimal? PriceOverride { get; set; }

    public SeatStatus Status { get; set; }
        = SeatStatus.Available;

    // Used later for concurrency protection
    public byte[] RowVersion { get; set; }
        = Array.Empty<byte>();

    // Navigation Properties
    public Event? Event { get; set; }

    public ICollection<BookingSeat> BookingSeats { get; set; }
        = new List<BookingSeat>();
}