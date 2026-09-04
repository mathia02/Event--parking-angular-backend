using EventParking.Models.Enums;

namespace EventParking.Models.Entities;

public class Customer
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    // Customer / Administrator
    public string Role { get; set; } = "Customer";

    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    // Email Verification
    public bool EmailVerified { get; set; } = false;

    public string? EmailVerificationTokenHash { get; set; }

    public DateTime? EmailVerificationTokenExpiresAt { get; set; }

    // Password Reset
    public string? PasswordResetTokenHash { get; set; }

    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public ICollection<Booking> Bookings { get; set; }
        = new List<Booking>();

    public ICollection<Payment> Payments { get; set; }
        = new List<Payment>();

    public ICollection<Notification> Notifications { get; set; }
        = new List<Notification>();
}