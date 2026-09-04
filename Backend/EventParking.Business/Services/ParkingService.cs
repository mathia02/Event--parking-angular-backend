using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Parking;
using EventParking.Models.Entities;
using EventParking.Models.Enums;

namespace EventParking.Business.Services;

public class ParkingService : IParkingService
{
    private readonly IParkingRepository _parkingRepository;
    private readonly IEventRepository _eventRepository;

    public ParkingService(
        IParkingRepository parkingRepository,
        IEventRepository eventRepository)
    {
        _parkingRepository = parkingRepository;
        _eventRepository = eventRepository;
    }

    public async Task<List<ParkingSlotDto>> GetByEventIdAsync(
        int eventId,
        bool availableOnly = false)
    {
        var eventEntity =
            await _eventRepository.GetByIdAsync(eventId);

        if (eventEntity == null)
        {
            throw new NotFoundException(
                $"Event with ID {eventId} was not found.");
        }

        var parkingSlots = availableOnly
            ? await _parkingRepository
                .GetAvailableByEventIdAsync(eventId)
            : await _parkingRepository
                .GetByEventIdAsync(eventId);

        return parkingSlots
            .Select(MapToDto)
            .ToList();
    }

    public async Task<ParkingSlotDto> GetByIdAsync(int id)
    {
        var parkingSlot =
            await _parkingRepository.GetByIdAsync(id);

        if (parkingSlot == null)
        {
            throw new NotFoundException(
                $"Parking slot with ID {id} was not found.");
        }

        return MapToDto(parkingSlot);
    }

    public async Task<ParkingSlotDto> CreateAsync(
        int eventId,
        ParkingSlotCreateDto dto)
    {
        var eventEntity =
            await _eventRepository.GetByIdAsync(eventId);

        if (eventEntity == null)
        {
            throw new NotFoundException(
                $"Event with ID {eventId} was not found.");
        }

        ValidateCreateDto(dto);

        var normalizedSlotNumber =
            dto.SlotNumber.Trim();

        var exists =
            await _parkingRepository
                .SlotNumberExistsAsync(
                    eventId,
                    normalizedSlotNumber);

        if (exists)
        {
            throw new ConflictException(
                $"Parking slot '{normalizedSlotNumber}' already exists for this event.");
        }

        var parkingSlot = new ParkingSlot
        {
            EventId = eventId,
            SlotNumber = normalizedSlotNumber,
            Zone = dto.Zone.Trim(),
            Fee = dto.Fee,
            Status = ParkingSlotStatus.Available
        };

        await _parkingRepository.AddAsync(parkingSlot);
        await _parkingRepository.SaveChangesAsync();

        return MapToDto(parkingSlot);
    }

    public async Task<ParkingSlotDto> UpdateAsync(
        int id,
        ParkingSlotUpdateDto dto)
    {
        var parkingSlot =
            await _parkingRepository.GetByIdAsync(id);

        if (parkingSlot == null)
        {
            throw new NotFoundException(
                $"Parking slot with ID {id} was not found.");
        }

        ValidateUpdateDto(dto);

        var hasActiveReservation =
            await _parkingRepository
                .HasActiveReservationAsync(id);

        if (hasActiveReservation)
        {
            throw new ConflictException(
                "This parking slot has an active reservation and cannot be modified.");
        }

        var normalizedSlotNumber =
            dto.SlotNumber.Trim();

        var duplicate =
            await _parkingRepository
                .SlotNumberExistsAsync(
                    parkingSlot.EventId,
                    normalizedSlotNumber,
                    parkingSlot.Id);

        if (duplicate)
        {
            throw new ConflictException(
                $"Parking slot '{normalizedSlotNumber}' already exists for this event.");
        }

        parkingSlot.SlotNumber =
            normalizedSlotNumber;

        parkingSlot.Zone =
            dto.Zone.Trim();

        parkingSlot.Fee =
            dto.Fee;

        parkingSlot.Status =
            dto.Status;

        _parkingRepository.Update(parkingSlot);

        await _parkingRepository.SaveChangesAsync();

        return MapToDto(parkingSlot);
    }

    public async Task DeleteAsync(int id)
    {
        var parkingSlot =
            await _parkingRepository.GetByIdAsync(id);

        if (parkingSlot == null)
        {
            throw new NotFoundException(
                $"Parking slot with ID {id} was not found.");
        }

        var hasActiveReservation =
            await _parkingRepository
                .HasActiveReservationAsync(id);

        if (hasActiveReservation)
        {
            throw new ConflictException(
                "This parking slot has an active reservation and cannot be deleted.");
        }

        _parkingRepository.Delete(parkingSlot);

        await _parkingRepository.SaveChangesAsync();
    }

    private static void ValidateCreateDto(
        ParkingSlotCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SlotNumber))
        {
            throw new ValidationException(
                "Parking slot number is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Zone))
        {
            throw new ValidationException(
                "Parking zone is required.");
        }

        if (dto.Fee < 0)
        {
            throw new ValidationException(
                "Parking fee cannot be negative.");
        }
    }

    private static void ValidateUpdateDto(
        ParkingSlotUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SlotNumber))
        {
            throw new ValidationException(
                "Parking slot number is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Zone))
        {
            throw new ValidationException(
                "Parking zone is required.");
        }

        if (dto.Fee < 0)
        {
            throw new ValidationException(
                "Parking fee cannot be negative.");
        }

        if (dto.Status != ParkingSlotStatus.Available &&
            dto.Status != ParkingSlotStatus.Unavailable)
        {
            throw new ValidationException(
                "Administrator can only manually set a parking slot as Available or Unavailable.");
        }
    }

    private static ParkingSlotDto MapToDto(
        ParkingSlot parkingSlot)
    {
        return new ParkingSlotDto
        {
            Id = parkingSlot.Id,
            EventId = parkingSlot.EventId,
            SlotNumber = parkingSlot.SlotNumber,

            // Entity allows null, DTO does not.
            Zone = parkingSlot.Zone ?? string.Empty,

            // Entity allows null, DTO does not.
            Fee = parkingSlot.Fee ?? 0m,

            Status = parkingSlot.Status
        };
    }
}