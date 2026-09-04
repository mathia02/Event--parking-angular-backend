namespace EventParking.Models.DTOs.Dashboard;

public class AdminDashboardDto
{
    public DateTime GeneratedAtUtc { get; set; }

    public int TotalCustomers { get; set; }

    public int TotalVenues { get; set; }

    public int TotalCategories { get; set; }

    public int TotalEvents { get; set; }

    public int UpcomingEvents { get; set; }

    public int TotalBookings { get; set; }

    public int PendingBookings { get; set; }

    public int ConfirmedBookings { get; set; }

    public int CancelledBookings { get; set; }

    public int ExpiredBookings { get; set; }

    public decimal TotalRevenue { get; set; }

    public int AvailableSeats { get; set; }

    public int HeldSeats { get; set; }

    public int BookedSeats { get; set; }

    public int AvailableParkingSlots { get; set; }

    public int HeldParkingSlots { get; set; }

    public int BookedParkingSlots { get; set; }

    public int UnavailableParkingSlots { get; set; }
}