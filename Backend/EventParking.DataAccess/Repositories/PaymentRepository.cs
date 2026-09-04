using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _context;

    public PaymentRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Payment>> GetAllAsync()
    {
        return await _context.Payments
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Booking)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Payment>>
        GetByCustomerIdAsync(int customerId)
    {
        return await _context.Payments
            .AsNoTracking()
            .Where(x =>
                x.CustomerId == customerId)
            .Include(x => x.Customer)
            .Include(x => x.Booking)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<Payment?> GetByIdAsync(int id)
    {
        return await _context.Payments
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Booking)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool>
        HasPaymentForBookingAsync(int bookingId)
    {
        return await _context.Payments
            .AnyAsync(x =>
                x.BookingId == bookingId);
    }

    public async Task<bool>
        TransactionReferenceExistsAsync(
            string transactionReference)
    {
        return await _context.Payments
            .AnyAsync(x =>
                x.TransactionReference ==
                transactionReference);
    }

    public async Task AddAsync(Payment payment)
    {
        await _context.Payments
            .AddAsync(payment);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context
            .SaveChangesAsync();
    }

    public async Task ExecuteInTransactionAsync(
        Func<Task> operation)
    {
        await using var transaction =
            await _context.Database
                .BeginTransactionAsync();

        try
        {
            await operation();

            await transaction
                .CommitAsync();
        }
        catch
        {
            await transaction
                .RollbackAsync();

            throw;
        }
    }
}