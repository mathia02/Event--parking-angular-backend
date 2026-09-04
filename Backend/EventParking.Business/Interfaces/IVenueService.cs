using EventParking.Models.DTOs.Venue;

namespace EventParking.Business.Interfaces;

public interface IVenueService
{
    Task<List<VenueDto>> GetAllAsync();

    Task<VenueDto> GetByIdAsync(int id);

    Task<List<VenueDto>> GetAvailableAsync(
        DateTime startDateTime,
        DateTime endDateTime,
        int? excludeEventId = null);

    Task<bool> IsAvailableAsync(
        int venueId,
        DateTime startDateTime,
        DateTime endDateTime,
        int? excludeEventId = null);

    Task<VenueDto> CreateAsync(VenueCreateDto dto);

    Task<VenueDto> UpdateAsync(
        int id,
        VenueUpdateDto dto);

    Task DeleteAsync(int id);
}