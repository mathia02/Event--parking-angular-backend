using EventParking.Business.Interfaces;

namespace EventParking.Api.BackgroundServices;

public class BookingExpiryBackgroundService
    : BackgroundService
{
    private static readonly TimeSpan CheckInterval =
        TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory
        _scopeFactory;

    private readonly ILogger<
        BookingExpiryBackgroundService>
        _logger;

    public BookingExpiryBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingExpiryBackgroundService> logger)
    {
        _scopeFactory =
            scopeFactory;

        _logger =
            logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Booking Expiry Background Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredBookingsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while processing expired bookings.");
            }

            try
            {
                await Task.Delay(
                    CheckInterval,
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation(
            "Booking Expiry Background Service stopped.");
    }

    private async Task ProcessExpiredBookingsAsync()
    {
        using var scope =
            _scopeFactory.CreateScope();

        var expiryService =
            scope.ServiceProvider
                .GetRequiredService<
                    IBookingExpiryService>();

        var expiredCount =
            await expiryService
                .ExpirePendingBookingsAsync();

        if (expiredCount > 0)
        {
            _logger.LogInformation(
                "{ExpiredCount} pending booking(s) expired and resources were released.",
                expiredCount);
        }
    }
}