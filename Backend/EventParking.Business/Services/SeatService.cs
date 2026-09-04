using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Seat;
using EventParking.Models.Entities;
using EventParking.Models.Enums;

namespace EventParking.Business.Services;

public class SeatService : ISeatService
{
    private readonly ISeatRepository _seatRepository;
    private readonly IEventRepository _eventRepository;

    public SeatService(
        ISeatRepository seatRepository,
        IEventRepository eventRepository)
    {
        _seatRepository = seatRepository;
        _eventRepository = eventRepository;
    }

    public async Task<List<SeatDto>>
        GetByEventIdAsync(
            int eventId,
            bool availableOnly = false)
    {
        await EnsureEventExistsAsync(eventId);

        var seats = availableOnly
            ? await _seatRepository
                .GetAvailableByEventIdAsync(eventId)
            : await _seatRepository
                .GetByEventIdAsync(eventId);

        return seats
            .Select(MapToDto)
            .ToList();
    }

    public async Task<SeatDto> GetByIdAsync(int id)
    {
        var seat =
            await _seatRepository.GetByIdAsync(id);

        if (seat is null)
        {
            throw new NotFoundException(
                $"Seat with ID {id} was not found.");
        }

        return MapToDto(seat);
    }

    public async Task<SeatDto> CreateAsync(
        int eventId,
        SeatCreateDto dto)
    {
        var eventEntity =
            await _eventRepository.GetByIdAsync(eventId);

        if (eventEntity is null)
        {
            throw new NotFoundException(
                $"Event with ID {eventId} was not found.");
        }

        ValidateSeatData(
            dto.SeatNumber,
            dto.RowLabel,
            dto.ColumnNumber,
            dto.PriceOverride);

        var currentSeatCount =
            await _seatRepository
                .GetSeatCountByEventAsync(eventId);

        if (currentSeatCount >= eventEntity.Capacity)
        {
            throw new ValidationException(
                $"Cannot create more seats. Event capacity is {eventEntity.Capacity}.");
        }

        var seatNumber =
            dto.SeatNumber.Trim();

        var exists =
            await _seatRepository
                .SeatNumberExistsAsync(
                    eventId,
                    seatNumber);

        if (exists)
        {
            throw new ConflictException(
                $"Seat number '{seatNumber}' already exists for this event.");
        }

        var seat = new Seat
        {
            EventId = eventId,

            SeatNumber = seatNumber,

            RowLabel =
                dto.RowLabel.Trim()
                    .ToUpperInvariant(),

            ColumnNumber =
                dto.ColumnNumber,

            SeatType =
                string.IsNullOrWhiteSpace(dto.SeatType)
                    ? null
                    : dto.SeatType.Trim(),

            PriceOverride =
                dto.PriceOverride,

            Status =
                SeatStatus.Available
        };

        await _seatRepository.AddAsync(seat);

        await _seatRepository.SaveChangesAsync();

        return MapToDto(seat);
    }

    public async Task<List<SeatDto>>
        GenerateSeatMapAsync(
            int eventId,
            int rows,
            int seatsPerRow,
            string? seatType = null,
            decimal? priceOverride = null)
    {
        var eventEntity =
            await _eventRepository.GetByIdAsync(eventId);

        if (eventEntity is null)
        {
            throw new NotFoundException(
                $"Event with ID {eventId} was not found.");
        }

        if (rows <= 0)
        {
            throw new ValidationException(
                "Rows must be greater than zero.");
        }

        if (seatsPerRow <= 0)
        {
            throw new ValidationException(
                "Seats per row must be greater than zero.");
        }

        if (priceOverride.HasValue &&
            priceOverride.Value < 0)
        {
            throw new ValidationException(
                "Seat price override cannot be negative.");
        }

        var existingSeatCount =
            await _seatRepository
                .GetSeatCountByEventAsync(eventId);

        if (existingSeatCount > 0)
        {
            throw new ConflictException(
                "A seat map already exists for this event.");
        }

        var totalSeats =
            checked(rows * seatsPerRow);

        if (totalSeats != eventEntity.Capacity)
        {
            throw new ValidationException(
                $"Generated seat count must exactly match event capacity of {eventEntity.Capacity}. Requested seat count is {totalSeats}.");
        }

        var seats = new List<Seat>();

        for (var rowIndex = 0;
             rowIndex < rows;
             rowIndex++)
        {
            var rowLabel =
                GetRowLabel(rowIndex);

            for (var column = 1;
                 column <= seatsPerRow;
                 column++)
            {
                seats.Add(
                    new Seat
                    {
                        EventId = eventId,

                        RowLabel = rowLabel,

                        ColumnNumber = column,

                        SeatNumber =
                            $"{rowLabel}{column}",

                        SeatType =
                            string.IsNullOrWhiteSpace(
                                seatType)
                                ? "Regular"
                                : seatType.Trim(),

                        PriceOverride =
                            priceOverride,

                        Status =
                            SeatStatus.Available
                    });
            }
        }

        await _seatRepository
            .AddRangeAsync(seats);

        await _seatRepository
            .SaveChangesAsync();

        return seats
            .Select(MapToDto)
            .ToList();
    }

    public async Task<SeatDto> UpdateAsync(
        int id,
        SeatUpdateDto dto)
    {
        var seat =
            await _seatRepository.GetByIdAsync(id);

        if (seat is null)
        {
            throw new NotFoundException(
                $"Seat with ID {id} was not found.");
        }

        ValidateSeatData(
            dto.SeatNumber,
            dto.RowLabel,
            dto.ColumnNumber,
            dto.PriceOverride);

        var hasActiveBooking =
            await _seatRepository
                .HasActiveBookingAsync(id);

        if (hasActiveBooking)
        {
            throw new ConflictException(
                "A seat with an active booking cannot be modified.");
        }

        var seatNumber =
            dto.SeatNumber.Trim();

        var exists =
            await _seatRepository
                .SeatNumberExistsAsync(
                    seat.EventId,
                    seatNumber,
                    id);

        if (exists)
        {
            throw new ConflictException(
                $"Seat number '{seatNumber}' already exists for this event.");
        }

        seat.SeatNumber =
            seatNumber;

        seat.RowLabel =
            dto.RowLabel.Trim()
                .ToUpperInvariant();

        seat.ColumnNumber =
            dto.ColumnNumber;

        seat.SeatType =
            string.IsNullOrWhiteSpace(dto.SeatType)
                ? null
                : dto.SeatType.Trim();

        seat.PriceOverride =
            dto.PriceOverride;

        _seatRepository.Update(seat);

        await _seatRepository.SaveChangesAsync();

        return MapToDto(seat);
    }

    public async Task DeleteAsync(int id)
    {
        var seat =
            await _seatRepository.GetByIdAsync(id);

        if (seat is null)
        {
            throw new NotFoundException(
                $"Seat with ID {id} was not found.");
        }

        var hasActiveBooking =
            await _seatRepository
                .HasActiveBookingAsync(id);

        if (hasActiveBooking)
        {
            throw new ConflictException(
                "A seat with an active booking cannot be deleted.");
        }

        _seatRepository.Delete(seat);

        await _seatRepository.SaveChangesAsync();
    }

    private async Task EnsureEventExistsAsync(
        int eventId)
    {
        var eventEntity =
            await _eventRepository
                .GetByIdAsync(eventId);

        if (eventEntity is null)
        {
            throw new NotFoundException(
                $"Event with ID {eventId} was not found.");
        }
    }

    private static void ValidateSeatData(
        string seatNumber,
        string rowLabel,
        int columnNumber,
        decimal? priceOverride)
    {
        if (string.IsNullOrWhiteSpace(
            seatNumber))
        {
            throw new ValidationException(
                "Seat number is required.");
        }

        if (string.IsNullOrWhiteSpace(
            rowLabel))
        {
            throw new ValidationException(
                "Row label is required.");
        }

        if (columnNumber <= 0)
        {
            throw new ValidationException(
                "Column number must be greater than zero.");
        }

        if (priceOverride.HasValue &&
            priceOverride.Value < 0)
        {
            throw new ValidationException(
                "Seat price override cannot be negative.");
        }
    }

    private static string GetRowLabel(
        int zeroBasedIndex)
    {
        var result = string.Empty;

        var number =
            zeroBasedIndex + 1;

        while (number > 0)
        {
            number--;

            result =
                (char)('A' + number % 26)
                + result;

            number /= 26;
        }

        return result;
    }

    private static SeatDto MapToDto(
        Seat seat)
    {
        return new SeatDto
        {
            Id = seat.Id,

            EventId = seat.EventId,

            SeatNumber =
                seat.SeatNumber,

            RowLabel =
                seat.RowLabel,

            ColumnNumber =
                seat.ColumnNumber,

            SeatType =
                seat.SeatType,

            PriceOverride =
                seat.PriceOverride,

            Status =
                seat.Status
        };
    }
}