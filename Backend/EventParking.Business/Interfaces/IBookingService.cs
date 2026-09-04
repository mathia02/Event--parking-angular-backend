using EventParking.Models.DTOs.Booking;

namespace EventParking.Business.Interfaces;

public interface IBookingService
{
    Task<List<BookingDetailsDto>> GetAllAsync();

    Task<List<BookingDetailsDto>> GetMyBookingsAsync(
        int customerId);

    Task<BookingDetailsDto> GetByIdAsync(
        int id,
        int requesterCustomerId,
        bool isAdministrator);

    Task<BookingDetailsDto> CreateAsync(
        int customerId,
        BookingCreateDto dto);

    Task<BookingDetailsDto> CancelAsync(
        int id,
        int requesterCustomerId,
        bool isAdministrator);
}