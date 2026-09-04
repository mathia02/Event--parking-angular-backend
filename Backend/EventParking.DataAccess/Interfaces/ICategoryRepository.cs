using EventParking.Models.Entities;

namespace EventParking.DataAccess.Interfaces;

public interface ICategoryRepository
{
    Task<List<EventCategory>> GetAllAsync();

    Task<EventCategory?> GetByIdAsync(int id);

    Task<bool> NameExistsAsync(
        string name,
        int? excludeCategoryId = null);

    Task<bool> HasEventsAsync(int categoryId);

    Task AddAsync(EventCategory category);

    void Update(EventCategory category);

    void Delete(EventCategory category);

    Task<int> SaveChangesAsync();
}