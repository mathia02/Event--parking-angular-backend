using System.ComponentModel.DataAnnotations;

namespace EventParking.Models.DTOs.Customer;

public class CustomerUpdateDto
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;
}