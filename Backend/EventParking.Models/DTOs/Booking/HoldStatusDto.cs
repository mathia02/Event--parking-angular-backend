namespace EventParking.Models.DTOs.Booking;

public class HoldStatusDto
{
    public bool IsPending { get; set; }

    public bool IsExpired { get; set; }

    public DateTime? HoldExpiresAt { get; set; }

    public int RemainingSeconds { get; set; }
}