using System.ComponentModel.DataAnnotations;

namespace EventParking.Models.DTOs.Seat;

public class SeatCreateDto
{
    [Required]
    [MaxLength(20)]
    public string SeatNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string RowLabel { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ColumnNumber { get; set; }

    [MaxLength(50)]
    public string? SeatType { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? PriceOverride { get; set; }
}