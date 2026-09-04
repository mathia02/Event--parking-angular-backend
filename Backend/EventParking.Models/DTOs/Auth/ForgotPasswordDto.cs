using System.ComponentModel.DataAnnotations;

namespace EventParking.Models.DTOs.Auth;

public class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}