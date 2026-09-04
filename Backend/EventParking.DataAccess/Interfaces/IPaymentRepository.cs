using EventParking.Models.Entities;

namespace EventParking.DataAccess.Interfaces;

public interface IPaymentRepository
{
    Task<List<Payment>> GetAllAsync();

    Task<List<Payment>> GetByCustomerIdAsync(
        int customerId);

    Task<Payment?> GetByIdAsync(int id);

    Task<bool> HasPaymentForBookingAsync(
        int bookingId);

    Task<bool> TransactionReferenceExistsAsync(
        string transactionReference);

    Task AddAsync(Payment payment);

    Task<int> SaveChangesAsync();

    Task ExecuteInTransactionAsync(
        Func<Task> operation);
}