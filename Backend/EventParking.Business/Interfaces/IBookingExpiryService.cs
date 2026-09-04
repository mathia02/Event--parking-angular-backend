namespace EventParking.Business.Interfaces;

public interface IBookingExpiryService
{
    Task<int> ExpirePendingBookingsAsync();
}