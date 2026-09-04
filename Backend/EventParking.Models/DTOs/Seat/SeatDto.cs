using EventParking.Models.Enums;

namespace EventParking.Models.DTOs.Seat;

public class SeatDto
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string SeatNumber { get; set; } = string.Empty;

    public string RowLabel { get; set; } = string.Empty;

    public int ColumnNumber { get; set; }

    public string? SeatType { get; set; }

    public decimal? PriceOverride { get; set; }

    public SeatStatus Status { get; set; }
}