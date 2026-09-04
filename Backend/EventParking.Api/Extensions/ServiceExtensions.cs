using EventParking.Api.BackgroundServices;
using EventParking.Business.Interfaces;
using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.DataAccess.Repositories;

namespace EventParking.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection
        AddApplicationServices(
            this IServiceCollection services)
    {
        // -------------------------
        // Repositories
        // -------------------------

        services.AddScoped<
            ICustomerRepository,
            CustomerRepository>();

        services.AddScoped<
            IVenueRepository,
            VenueRepository>();

        services.AddScoped<
            ICategoryRepository,
            CategoryRepository>();

        services.AddScoped<
            IEventRepository,
            EventRepository>();

        services.AddScoped<
            ISeatRepository,
            SeatRepository>();

        services.AddScoped<
            IParkingRepository,
            ParkingRepository>();

        services.AddScoped<
            IBookingRepository,
            BookingRepository>();

        services.AddScoped<
            IPaymentRepository,
            PaymentRepository>();

        services.AddScoped<
            INotificationRepository,
            NotificationRepository>();

        services.AddScoped<
            IDashboardRepository,
            DashboardRepository>();

        // -------------------------
        // Business Services
        // -------------------------

        services.AddScoped<
            ICustomerService,
            CustomerService>();

        services.AddScoped<
            IAuthService,
            AuthService>();

        services.AddScoped<
            ITokenService,
            TokenService>();

        services.AddScoped<
            IEmailService,
            EmailService>();

        services.AddScoped<
            IVenueService,
            VenueService>();

        services.AddScoped<
            ICategoryService,
            CategoryService>();

        services.AddScoped<
            IEventService,
            EventService>();

        services.AddScoped<
            ISeatService,
            SeatService>();

        services.AddScoped<
            IParkingService,
            ParkingService>();

        services.AddScoped<
            IBookingService,
            BookingService>();

        services.AddScoped<
            IBookingExpiryService,
            BookingExpiryService>();

        services.AddScoped<
            IPaymentService,
            PaymentService>();

        services.AddScoped<
            INotificationService,
            NotificationService>();

        services.AddScoped<
            IDashboardService,
            DashboardService>();

        // -------------------------
        // Background Services
        // -------------------------

        services.AddHostedService<
            BookingExpiryBackgroundService>();

        services.AddHostedService<
            EventReminderBackgroundService>();

        return services;
    }
}