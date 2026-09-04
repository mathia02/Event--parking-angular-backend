using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Customer;
using EventParking.Models.Entities;
using EventParking.Models.Enums;

namespace EventParking.Business.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<List<CustomerDto>> GetCustomersAsync(string? search)
    {
        var customers = await _customerRepository.SearchAsync(search);

        return customers
            .Select(MapToDto)
            .ToList();
    }

    public async Task<CustomerDto> GetByIdAsync(int id)
    {
        var customer = await _customerRepository
            .GetByIdWithBookingsAsync(id);

        if (customer is null)
        {
            throw new NotFoundException(
                $"Customer with ID {id} was not found.");
        }

        return MapToDto(customer);
    }

    public async Task<CustomerDto> UpdateProfileAsync(
        int id,
        CustomerUpdateDto dto)
    {
        var customer = await _customerRepository.GetByIdAsync(id);

        if (customer is null)
        {
            throw new NotFoundException(
                $"Customer with ID {id} was not found.");
        }

        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            throw new ValidationException(
                "Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Phone))
        {
            throw new ValidationException(
                "Phone number is required.");
        }

        customer.FullName = dto.FullName.Trim();
        customer.Phone = dto.Phone.Trim();
        customer.UpdatedAt = DateTime.UtcNow;

        _customerRepository.Update(customer);

        await _customerRepository.SaveChangesAsync();

        return MapToDto(customer);
    }

    public async Task DeactivateAsync(int id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);

        if (customer is null)
        {
            throw new NotFoundException(
                $"Customer with ID {id} was not found.");
        }

        if (customer.Status == CustomerStatus.Deactivated)
        {
            throw new ValidationException(
                "Customer account is already deactivated.");
        }

        var hasActiveFutureBookings =
            await _customerRepository
                .HasActiveFutureBookingsAsync(
                    id,
                    DateTime.UtcNow);

        if (hasActiveFutureBookings)
        {
            throw new ValidationException(
                "Customer cannot be deactivated because they have active future bookings.");
        }

        customer.Status = CustomerStatus.Deactivated;
        customer.UpdatedAt = DateTime.UtcNow;

        _customerRepository.Update(customer);

        await _customerRepository.SaveChangesAsync();
    }

    public async Task<CustomerDto> ReactivateAsync(int id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);

        if (customer is null)
        {
            throw new NotFoundException(
                $"Customer with ID {id} was not found.");
        }

        if (customer.Status == CustomerStatus.Active)
        {
            throw new ValidationException(
                "Customer account is already active.");
        }

        customer.Status = CustomerStatus.Active;
        customer.UpdatedAt = DateTime.UtcNow;

        _customerRepository.Update(customer);

        await _customerRepository.SaveChangesAsync();

        return MapToDto(customer);
    }

    private static CustomerDto MapToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            FullName = customer.FullName,
            Email = customer.Email,
            Phone = customer.Phone,
            Role = customer.Role,
            Status = customer.Status,
            EmailVerified = customer.EmailVerified,
            CreatedAt = customer.CreatedAt
        };
    }
}