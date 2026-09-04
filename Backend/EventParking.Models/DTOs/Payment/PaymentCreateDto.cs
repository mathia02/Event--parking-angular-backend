using System.ComponentModel.DataAnnotations;
using EventParking.Models.Enums;

namespace EventParking.Models.DTOs.Payment;

public class PaymentCreateDto
{
    [Range(
        1,
        3,
        ErrorMessage = "Please select a valid payment method.")]
    public PaymentMethod PaymentMethod { get; set; }
}