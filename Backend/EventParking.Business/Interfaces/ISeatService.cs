using EventParking.Models.DTOs.Seat;

namespace EventParking.Business.Interfaces;

public interface ISeatService
{
    Task<List<SeatDto>> GetByEventIdAsync(
        int eventId,
        bool availableOnly = false);

    Task<SeatDto> GetByIdAsync(int id);

    Task<SeatDto> CreateAsync(
        int eventId,
        SeatCreateDto dto);

    Task<List<SeatDto>> GenerateSeatMapAsync(
        int eventId,
        int rows,
        int seatsPerRow,
        string? seatType = null,
        decimal? priceOverride = null);

    Task<SeatDto> UpdateAsync(
        int id,
        SeatUpdateDto dto);

    Task DeleteAsync(int id);
}