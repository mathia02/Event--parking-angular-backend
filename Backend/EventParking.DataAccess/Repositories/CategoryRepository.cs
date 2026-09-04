using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;

    public CategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<EventCategory>> GetAllAsync()
    {
        return await _context.EventCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<EventCategory?> GetByIdAsync(int id)
    {
        return await _context.EventCategories
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> NameExistsAsync(
        string name,
        int? excludeCategoryId = null)
    {
        var normalizedName = name.Trim();

        var query = _context.EventCategories
            .AsNoTracking()
            .Where(c => c.Name == normalizedName);

        if (excludeCategoryId.HasValue)
        {
            query = query.Where(c =>
                c.Id != excludeCategoryId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> HasEventsAsync(int categoryId)
    {
        return await _context.Events
            .AsNoTracking()
            .AnyAsync(e => e.CategoryId == categoryId);
    }

    public async Task AddAsync(EventCategory category)
    {
        await _context.EventCategories.AddAsync(category);
    }

    public void Update(EventCategory category)
    {
        _context.EventCategories.Update(category);
    }

    public void Delete(EventCategory category)
    {
        _context.EventCategories.Remove(category);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}