using EventParking.Models.DTOs.Customer;

namespace EventParking.Business.Interfaces;

public interface ICustomerService
{
    Task<List<CustomerDto>> GetCustomersAsync(string? search);

    Task<CustomerDto> GetByIdAsync(int id);

    Task<CustomerDto> UpdateProfileAsync(
        int id,
        CustomerUpdateDto dto);

    Task DeactivateAsync(int id);

    Task<CustomerDto> ReactivateAsync(int id);
}