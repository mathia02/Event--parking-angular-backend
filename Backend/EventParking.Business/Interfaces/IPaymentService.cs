using EventParking.Models.DTOs.Payment;

namespace EventParking.Business.Interfaces;

public interface IPaymentService
{
    Task<List<PaymentDto>> GetAllAsync();

    Task<List<PaymentDto>> GetMyPaymentsAsync(
        int customerId);

    Task<PaymentDto> GetByIdAsync(
        int id,
        int requesterCustomerId,
        bool isAdministrator);

    Task<PaymentDto> CreateAsync(
        int bookingId,
        int customerId,
        PaymentCreateDto dto);
}