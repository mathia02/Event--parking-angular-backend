using EventParking.Models.Enums;

namespace EventParking.Models.DTOs.Customer;

public class CustomerDto
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public CustomerStatus Status { get; set; }

    public bool EmailVerified { get; set; }

    public DateTime CreatedAt { get; set; }
}