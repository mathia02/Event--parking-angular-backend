namespace EventParking.Models.DTOs.Dashboard;

public class CustomerDashboardDto
{
    public DateTime GeneratedAtUtc { get; set; }

    public int TotalBookings { get; set; }

    public int PendingBookings { get; set; }

    public int ConfirmedBookings { get; set; }

    public int CancelledBookings { get; set; }

    public int ExpiredBookings { get; set; }

    public int UpcomingConfirmedBookings { get; set; }

    public decimal TotalPaid { get; set; }

    public int UnreadNotifications { get; set; }
}