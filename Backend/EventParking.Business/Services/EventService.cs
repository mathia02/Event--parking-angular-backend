using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.DataAccess.Repositories;
using EventParking.Models.DTOs.Event;
using EventParking.Models.Entities;

namespace EventParking.Business.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly ICategoryRepository _categoryRepository;

    public EventService(
        IEventRepository eventRepository,
        IVenueRepository venueRepository,
        ICategoryRepository categoryRepository)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<List<EventDto>> GetAllAsync(
        string? name,
        DateOnly? date,
        int? venueId,
        int? categoryId)
    {
        var events =
            await _eventRepository.GetAllAsync(
                name,
                date,
                venueId,
                categoryId);

        return events
            .Select(MapToDto)
            .ToList();
    }

    public async Task<EventDto> GetByIdAsync(int id)
    {
        var eventEntity =
            await _eventRepository.GetByIdAsync(id);

        if (eventEntity is null)
        {
            throw new NotFoundException(
                $"Event with ID {id} was not found.");
        }

        return MapToDto(eventEntity);
    }

    public async Task<EventDto> CreateAsync(
        EventCreateDto dto)
    {
        ValidateEventData(
            dto.Name,
            dto.StartDateTime,
            dto.EndDateTime,
            dto.TicketPrice,
            dto.ParkingFee,
            dto.Capacity);

        Event? createdEvent = null;

        await _eventRepository
            .ExecuteInTransactionAsync(
                async () =>
                {
                    var venue =
                        await _venueRepository
                            .GetByIdAsync(dto.VenueId);

                    if (venue is null)
                    {
                        throw new NotFoundException(
                            $"Venue with ID {dto.VenueId} was not found.");
                    }

                    var category =
                        await _categoryRepository
                            .GetByIdAsync(dto.CategoryId);

                    if (category is null)
                    {
                        throw new NotFoundException(
                            $"Category with ID {dto.CategoryId} was not found.");
                    }

                    if (dto.Capacity > venue.Capacity)
                    {
                        throw new ValidationException(
                            $"Event capacity cannot exceed venue capacity of {venue.Capacity}.");
                    }

                    var hasOverlap =
                        await _eventRepository
                            .HasOverlapAsync(
                                dto.VenueId,
                                dto.StartDateTime,
                                dto.EndDateTime);

                    if (hasOverlap)
                    {
                        throw new ConflictException(
                            "The selected venue already has an overlapping event during this time period.");
                    }

                    createdEvent = new Event
                    {
                        Name = dto.Name.Trim(),

                        Description =
                            string.IsNullOrWhiteSpace(
                                dto.Description)
                                ? null
                                : dto.Description.Trim(),

                        VenueId = dto.VenueId,
                        CategoryId = dto.CategoryId,

                        StartDateTime =
                            dto.StartDateTime,

                        EndDateTime =
                            dto.EndDateTime,

                        TicketPrice =
                            dto.TicketPrice,

                        ParkingFee =
                            dto.ParkingFee,

                        Capacity =
                            dto.Capacity,

                        CreatedAt =
                            DateTime.UtcNow,

                        Venue = venue,
                        Category = category
                    };

                    await _eventRepository
                        .AddAsync(createdEvent);

                    await _eventRepository
                        .SaveChangesAsync();
                });

        return MapToDto(createdEvent!);
    }

    public async Task<EventDto> UpdateAsync(
        int id,
        EventUpdateDto dto)
    {
        ValidateEventData(
            dto.Name,
            dto.StartDateTime,
            dto.EndDateTime,
            dto.TicketPrice,
            dto.ParkingFee,
            dto.Capacity);

        Event? updatedEvent = null;

        await _eventRepository
            .ExecuteInTransactionAsync(
                async () =>
                {
                    var eventEntity =
                        await _eventRepository
                            .GetByIdAsync(id);

                    if (eventEntity is null)
                    {
                        throw new NotFoundException(
                            $"Event with ID {id} was not found.");
                    }

                    var venue =
                        await _venueRepository
                            .GetByIdAsync(dto.VenueId);

                    if (venue is null)
                    {
                        throw new NotFoundException(
                            $"Venue with ID {dto.VenueId} was not found.");
                    }

                    var category =
                        await _categoryRepository
                            .GetByIdAsync(dto.CategoryId);

                    if (category is null)
                    {
                        throw new NotFoundException(
                            $"Category with ID {dto.CategoryId} was not found.");
                    }

                    if (dto.Capacity > venue.Capacity)
                    {
                        throw new ValidationException(
                            $"Event capacity cannot exceed venue capacity of {venue.Capacity}.");
                    }

                    var bookedSeatCount =
                        await _eventRepository
                            .GetBookedSeatCountAsync(id);

                    if (dto.Capacity < bookedSeatCount)
                    {
                        throw new ValidationException(
                            $"Event capacity cannot be reduced below the currently booked seat count of {bookedSeatCount}.");
                    }

                    var hasBookings =
                        await _eventRepository
                            .HasAnyBookingsAsync(id);

                    if (hasBookings &&
                        dto.TicketPrice !=
                        eventEntity.TicketPrice)
                    {
                        throw new ConflictException(
                            "Ticket price cannot be changed after bookings exist for this event.");
                    }

                    var hasOverlap =
                        await _eventRepository
                            .HasOverlapAsync(
                                dto.VenueId,
                                dto.StartDateTime,
                                dto.EndDateTime,
                                id);

                    if (hasOverlap)
                    {
                        throw new ConflictException(
                            "The selected venue already has an overlapping event during this time period.");
                    }

                    eventEntity.Name =
                        dto.Name.Trim();

                    eventEntity.Description =
                        string.IsNullOrWhiteSpace(
                            dto.Description)
                            ? null
                            : dto.Description.Trim();

                    eventEntity.VenueId =
                        dto.VenueId;

                    eventEntity.CategoryId =
                        dto.CategoryId;

                    eventEntity.StartDateTime =
                        dto.StartDateTime;

                    eventEntity.EndDateTime =
                        dto.EndDateTime;

                    eventEntity.TicketPrice =
                        dto.TicketPrice;

                    eventEntity.ParkingFee =
                        dto.ParkingFee;

                    eventEntity.Capacity =
                        dto.Capacity;

                    eventEntity.UpdatedAt =
                        DateTime.UtcNow;

                    eventEntity.Venue = venue;
                    eventEntity.Category = category;

                    _eventRepository.Update(eventEntity);

                    await _eventRepository
                        .SaveChangesAsync();

                    updatedEvent = eventEntity;
                });

        return MapToDto(updatedEvent!);
    }

    public async Task DeleteAsync(int id)
    {
        await _eventRepository
            .ExecuteInTransactionAsync(
                async () =>
                {
                    var eventEntity =
                        await _eventRepository
                            .GetByIdAsync(id);

                    if (eventEntity is null)
                    {
                        throw new NotFoundException(
                            $"Event with ID {id} was not found.");
                    }

                    var hasActiveBookings =
                        await _eventRepository
                            .HasActiveBookingsAsync(id);

                    if (hasActiveBookings)
                    {
                        throw new ConflictException(
                            "Event cannot be deleted because it has active bookings.");
                    }

                    _eventRepository.Delete(
                        eventEntity);

                    await _eventRepository
                        .SaveChangesAsync();
                });
    }

    private static void ValidateEventData(
        string name,
        DateTime startDateTime,
        DateTime endDateTime,
        decimal ticketPrice,
        decimal parkingFee,
        int capacity)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException(
                "Event name is required.");
        }

        if (endDateTime <= startDateTime)
        {
            throw new ValidationException(
                "Event end date/time must be later than start date/time.");
        }

        if (ticketPrice < 0)
        {
            throw new ValidationException(
                "Ticket price cannot be negative.");
        }

        if (parkingFee < 0)
        {
            throw new ValidationException(
                "Parking fee cannot be negative.");
        }

        if (capacity <= 0)
        {
            throw new ValidationException(
                "Event capacity must be greater than zero.");
        }
    }

    private static EventDto MapToDto(
        Event eventEntity)
    {
        return new EventDto
        {
            Id = eventEntity.Id,

            Name = eventEntity.Name,

            Description =
                eventEntity.Description,

            VenueId =
                eventEntity.VenueId,

            VenueName =
                eventEntity.Venue?.Name
                ?? string.Empty,

            CategoryId =
                eventEntity.CategoryId,

            CategoryName =
                eventEntity.Category?.Name
                ?? string.Empty,

            StartDateTime =
                eventEntity.StartDateTime,

            EndDateTime =
                eventEntity.EndDateTime,

            TicketPrice =
                eventEntity.TicketPrice,

            ParkingFee =
                eventEntity.ParkingFee,

            Capacity =
                eventEntity.Capacity
        };
    }
}