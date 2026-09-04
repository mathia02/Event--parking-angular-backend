using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;

    public CustomerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Customer>> GetAllAsync()
    {
        return await _context.Customers
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Customer>> SearchAsync(string? search)
    {
        var query = _context.Customers
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim();

            query = query.Where(c =>
                c.FullName.Contains(searchTerm) ||
                c.Email.Contains(searchTerm));
        }

        return await query
            .OrderBy(c => c.FullName)
            .ToListAsync();
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Customer?> GetByIdWithBookingsAsync(int id)
    {
        return await _context.Customers
            .AsNoTracking()
            .Include(c => c.Bookings)
                .ThenInclude(b => b.Event)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Customer?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim();

        return await _context.Customers
            .FirstOrDefaultAsync(c =>
                c.Email == normalizedEmail);
    }

    public async Task<Customer?>
        GetByEmailVerificationTokenHashAsync(string tokenHash)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c =>
                c.EmailVerificationTokenHash == tokenHash);
    }

    public async Task<Customer?>
        GetByPasswordResetTokenHashAsync(string tokenHash)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c =>
                c.PasswordResetTokenHash == tokenHash);
    }

    public async Task<bool> EmailExistsAsync(
        string email,
        int? excludeCustomerId = null)
    {
        var normalizedEmail = email.Trim();

        var query = _context.Customers
            .AsNoTracking()
            .Where(c => c.Email == normalizedEmail);

        if (excludeCustomerId.HasValue)
        {
            query = query.Where(c =>
                c.Id != excludeCustomerId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> HasActiveFutureBookingsAsync(
        int customerId,
        DateTime currentUtcDateTime)
    {
        return await _context.Bookings
            .AsNoTracking()
            .AnyAsync(b =>
                b.CustomerId == customerId &&
                b.Event != null &&
                b.Event.StartDateTime > currentUtcDateTime &&
                (
                    b.Status == BookingStatus.Pending ||
                    b.Status == BookingStatus.Confirmed
                ));
    }

    public async Task AddAsync(Customer customer)
    {
        await _context.Customers.AddAsync(customer);
    }

    public void Update(Customer customer)
    {
        _context.Customers.Update(customer);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}