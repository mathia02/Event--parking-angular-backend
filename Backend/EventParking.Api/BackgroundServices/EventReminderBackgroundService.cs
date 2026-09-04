using EventParking.Business.Interfaces;

namespace EventParking.Api.BackgroundServices;

public class EventReminderBackgroundService
    : BackgroundService
{
    private static readonly TimeSpan
        CheckInterval =
            TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory
        _scopeFactory;

    private readonly ILogger<
        EventReminderBackgroundService>
        _logger;

    public EventReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<EventReminderBackgroundService> logger)
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
            "Event Reminder Background Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CreateRemindersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while creating event reminders.");
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
    }

    private async Task CreateRemindersAsync()
    {
        using var scope =
            _scopeFactory.CreateScope();

        var service =
            scope.ServiceProvider
                .GetRequiredService<
                    INotificationService>();

        var now =
            DateTime.UtcNow;

        var count =
            await service
                .CreateEventRemindersAsync(
                    now,
                    now.AddHours(24));

        if (count > 0)
        {
            _logger.LogInformation(
                "{ReminderCount} event reminder notification(s) created.",
                count);
        }
    }
}