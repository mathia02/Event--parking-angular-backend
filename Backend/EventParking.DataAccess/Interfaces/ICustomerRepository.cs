using EventParking.Models.Entities;

namespace EventParking.DataAccess.Interfaces;

public interface ICustomerRepository
{
    Task<List<Customer>> GetAllAsync();

    Task<List<Customer>> SearchAsync(string? search);

    Task<Customer?> GetByIdAsync(int id);

    Task<Customer?> GetByIdWithBookingsAsync(int id);

    Task<Customer?> GetByEmailAsync(string email);

    Task<Customer?> GetByEmailVerificationTokenHashAsync(
        string tokenHash);

    Task<Customer?> GetByPasswordResetTokenHashAsync(
        string tokenHash);

    Task<bool> EmailExistsAsync(
        string email,
        int? excludeCustomerId = null);

    Task<bool> HasActiveFutureBookingsAsync(
        int customerId,
        DateTime currentUtcDateTime);

    Task AddAsync(Customer customer);

    void Update(Customer customer);

    Task<int> SaveChangesAsync();
}