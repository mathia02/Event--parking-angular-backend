using System.ComponentModel.DataAnnotations;

namespace EventParking.Models.DTOs.Category;

public class CategoryCreateDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}