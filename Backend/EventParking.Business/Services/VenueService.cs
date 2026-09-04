using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Venue;
using EventParking.Models.Entities;

namespace EventParking.Business.Services;

public class VenueService : IVenueService
{
    private readonly IVenueRepository _venueRepository;

    public VenueService(IVenueRepository venueRepository)
    {
        _venueRepository = venueRepository;
    }

    public async Task<List<VenueDto>> GetAllAsync()
    {
        var venues =
            await _venueRepository.GetAllAsync();

        return venues
            .Select(MapToDto)
            .ToList();
    }

    public async Task<VenueDto> GetByIdAsync(int id)
    {
        var venue =
            await _venueRepository.GetByIdAsync(id);

        if (venue is null)
        {
            throw new NotFoundException(
                $"Venue with ID {id} was not found.");
        }

        return MapToDto(venue);
    }

    public async Task<List<VenueDto>> GetAvailableAsync(
        DateTime startDateTime,
        DateTime endDateTime,
        int? excludeEventId = null)
    {
        ValidateDateTimeRange(
            startDateTime,
            endDateTime);

        var venues =
            await _venueRepository.GetAvailableAsync(
                startDateTime,
                endDateTime,
                excludeEventId);

        return venues
            .Select(MapToDto)
            .ToList();
    }

    public async Task<bool> IsAvailableAsync(
        int venueId,
        DateTime startDateTime,
        DateTime endDateTime,
        int? excludeEventId = null)
    {
        ValidateDateTimeRange(
            startDateTime,
            endDateTime);

        var venue =
            await _venueRepository.GetByIdAsync(venueId);

        if (venue is null)
        {
            throw new NotFoundException(
                $"Venue with ID {venueId} was not found.");
        }

        return await _venueRepository
            .IsAvailableAsync(
                venueId,
                startDateTime,
                endDateTime,
                excludeEventId);
    }

    public async Task<VenueDto> CreateAsync(
        VenueCreateDto dto)
    {
        ValidateVenue(
            dto.Name,
            dto.Address,
            dto.Capacity);

        var venue = new Venue
        {
            Name = dto.Name.Trim(),
            Address = dto.Address.Trim(),
            Capacity = dto.Capacity,
            CreatedAt = DateTime.UtcNow
        };

        await _venueRepository.AddAsync(venue);

        await _venueRepository.SaveChangesAsync();

        return MapToDto(venue);
    }

    public async Task<VenueDto> UpdateAsync(
        int id,
        VenueUpdateDto dto)
    {
        ValidateVenue(
            dto.Name,
            dto.Address,
            dto.Capacity);

        var venue =
            await _venueRepository.GetByIdAsync(id);

        if (venue is null)
        {
            throw new NotFoundException(
                $"Venue with ID {id} was not found.");
        }

        venue.Name = dto.Name.Trim();
        venue.Address = dto.Address.Trim();
        venue.Capacity = dto.Capacity;
        venue.UpdatedAt = DateTime.UtcNow;

        _venueRepository.Update(venue);

        await _venueRepository.SaveChangesAsync();

        return MapToDto(venue);
    }

    public async Task DeleteAsync(int id)
    {
        var venue =
            await _venueRepository.GetByIdAsync(id);

        if (venue is null)
        {
            throw new NotFoundException(
                $"Venue with ID {id} was not found.");
        }

        var hasUpcomingEvents =
            await _venueRepository
                .HasUpcomingEventsAsync(
                    id,
                    DateTime.UtcNow);

        if (hasUpcomingEvents)
        {
            throw new ConflictException(
                "Venue cannot be deleted because it has current or upcoming events.");
        }

        _venueRepository.Delete(venue);

        await _venueRepository.SaveChangesAsync();
    }

    private static void ValidateVenue(
        string name,
        string address,
        int capacity)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException(
                "Venue name is required.");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ValidationException(
                "Venue address is required.");
        }

        if (capacity <= 0)
        {
            throw new ValidationException(
                "Venue capacity must be greater than zero.");
        }
    }

    private static void ValidateDateTimeRange(
        DateTime startDateTime,
        DateTime endDateTime)
    {
        if (endDateTime <= startDateTime)
        {
            throw new ValidationException(
                "End date/time must be later than start date/time.");
        }
    }

    private static VenueDto MapToDto(Venue venue)
    {
        return new VenueDto
        {
            Id = venue.Id,
            Name = venue.Name,
            Address = venue.Address,
            Capacity = venue.Capacity
        };
    }
}