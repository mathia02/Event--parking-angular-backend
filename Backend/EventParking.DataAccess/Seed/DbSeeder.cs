using EventParking.DataAccess.Context;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        string? adminPassword,
        string? customerPassword)
    {
        // =========================================================
        // DEVELOPMENT DATABASE MIGRATIONS
        // Program.cs calls this seeder only in Development.
        // =========================================================

        var pendingMigrations =
            await context.Database
                .GetPendingMigrationsAsync();

        if (pendingMigrations.Any())
        {
            await context.Database
                .MigrateAsync();
        }

        // =========================================================
        // PASSWORD VALIDATION
        // =========================================================

        if (string.IsNullOrWhiteSpace(
                adminPassword))
        {
            throw new InvalidOperationException(
                "Seed:AdminPassword is not configured. " +
                "Store it using .NET User Secrets.");
        }

        if (string.IsNullOrWhiteSpace(
                customerPassword))
        {
            throw new InvalidOperationException(
                "Seed:CustomerPassword is not configured. " +
                "Store it using .NET User Secrets.");
        }

        // =========================================================
        // ADMIN
        // =========================================================

        const string adminEmail =
            "admin@test.com";

        var admin =
            await context.Customers
                .FirstOrDefaultAsync(x =>
                    x.Email == adminEmail);

        if (admin == null)
        {
            admin =
                new Customer
                {
                    FullName =
                        "System Administrator",

                    Email =
                        adminEmail,

                    Phone =
                        "0770000001",

                    PasswordHash =
                        BCrypt.Net.BCrypt
                            .HashPassword(
                                adminPassword),

                    Role =
                        "Administrator",

                    Status =
                        CustomerStatus.Active,

                    EmailVerified =
                        true,

                    CreatedAt =
                        DateTime.UtcNow
                };

            await context.Customers
                .AddAsync(admin);
        }
        else
        {
            // Development seed account:
            // User Secret password is authoritative.

            var passwordMatches =
                !string.IsNullOrWhiteSpace(
                    admin.PasswordHash) &&
                BCrypt.Net.BCrypt.Verify(
                    adminPassword,
                    admin.PasswordHash);

            if (!passwordMatches)
            {
                admin.PasswordHash =
                    BCrypt.Net.BCrypt
                        .HashPassword(
                            adminPassword);
            }

            admin.Role =
                "Administrator";

            admin.Status =
                CustomerStatus.Active;

            admin.EmailVerified =
                true;
        }

        await context.SaveChangesAsync();

        // =========================================================
        // DEFAULT CUSTOMER
        // =========================================================

        const string customerEmail =
            "customer1@test.com";

        var customer =
            await context.Customers
                .FirstOrDefaultAsync(x =>
                    x.Email ==
                    customerEmail);

        if (customer == null)
        {
            customer =
                new Customer
                {
                    FullName =
                        "Test Customer",

                    Email =
                        customerEmail,

                    Phone =
                        "0770000002",

                    PasswordHash =
                        BCrypt.Net.BCrypt
                            .HashPassword(
                                customerPassword),

                    Role =
                        "Customer",

                    Status =
                        CustomerStatus.Active,

                    EmailVerified =
                        true,

                    CreatedAt =
                        DateTime.UtcNow
                };

            await context.Customers
                .AddAsync(customer);
        }
        else
        {
            var passwordMatches =
                !string.IsNullOrWhiteSpace(
                    customer.PasswordHash) &&
                BCrypt.Net.BCrypt.Verify(
                    customerPassword,
                    customer.PasswordHash);

            if (!passwordMatches)
            {
                customer.PasswordHash =
                    BCrypt.Net.BCrypt
                        .HashPassword(
                            customerPassword);
            }

            customer.Role =
                "Customer";

            customer.Status =
                CustomerStatus.Active;

            customer.EmailVerified =
                true;
        }

        await context.SaveChangesAsync();

        // =========================================================
        // VENUE
        // =========================================================

        const string venueName =
            "Vavuniya Convention Centre";

        var venue =
            await context.Venues
                .FirstOrDefaultAsync(x =>
                    x.Name == venueName);

        if (venue == null)
        {
            venue =
                new Venue
                {
                    Name =
                        venueName,

                    Address =
                        "Vavuniya, Sri Lanka",

                    Capacity =
                        300,

                    CreatedAt =
                        DateTime.UtcNow
                };

            await context.Venues
                .AddAsync(venue);

            await context
                .SaveChangesAsync();
        }

        // =========================================================
        // CATEGORY
        // =========================================================

        const string categoryName =
            "Technology";

        var category =
            await context.EventCategories
                .FirstOrDefaultAsync(x =>
                    x.Name ==
                    categoryName);

        if (category == null)
        {
            category =
                new EventCategory
                {
                    Name =
                        categoryName,

                    Description =
                        "Technology related events and conferences.",

                    CreatedAt =
                        DateTime.UtcNow
                };

            await context.EventCategories
                .AddAsync(category);

            await context
                .SaveChangesAsync();
        }

        // =========================================================
        // DEMO EVENT
        // Fresh databases always receive a future event.
        // No hardcoded calendar date.
        // =========================================================

        const string eventName =
            "Tech Conference Demo";

        var eventEntity =
            await context.Events
                .FirstOrDefaultAsync(x =>
                    x.Name ==
                    eventName);

        if (eventEntity == null)
        {
            var demoStart =
                DateTime.UtcNow
                    .Date
                    .AddDays(30)
                    .AddHours(10);

            var demoEnd =
                demoStart
                    .AddHours(2);

            eventEntity =
                new Event
                {
                    Name =
                        eventName,

                    Description =
                        "Demo technology conference for development and testing.",

                    VenueId =
                        venue.Id,

                    CategoryId =
                        category.Id,

                    StartDateTime =
                        demoStart,

                    EndDateTime =
                        demoEnd,

                    TicketPrice =
                        2500m,

                    ParkingFee =
                        500m,

                    Capacity =
                        300,

                    CreatedAt =
                        DateTime.UtcNow
                };

            await context.Events
                .AddAsync(eventEntity);

            await context
                .SaveChangesAsync();
        }

        // =========================================================
        // SEATS
        // 15 ROWS x 20 SEATS = 300
        // =========================================================

        var existingSeatCount =
            await context.Seats
                .CountAsync(x =>
                    x.EventId ==
                    eventEntity.Id);

        if (existingSeatCount == 0)
        {
            var seats =
                new List<Seat>();

            const int rows = 15;
            const int columns = 20;

            for (var row = 0;
                 row < rows;
                 row++)
            {
                var rowLabel =
                    ((char)('A' + row))
                    .ToString();

                for (var column = 1;
                     column <= columns;
                     column++)
                {
                    var seatNumber =
                        $"{rowLabel}{column}";

                    var isVip =
                        row < 2;

                    seats.Add(
                        new Seat
                        {
                            EventId =
                                eventEntity.Id,

                            SeatNumber =
                                seatNumber,

                            RowLabel =
                                rowLabel,

                            ColumnNumber =
                                column,

                            SeatType =
                                isVip
                                    ? "VIP"
                                    : "Regular",

                            PriceOverride =
                                isVip
                                    ? 3500m
                                    : null,

                            Status =
                                SeatStatus.Available
                        });
                }
            }

            await context.Seats
                .AddRangeAsync(seats);

            await context
                .SaveChangesAsync();
        }

        // =========================================================
        // PARKING SLOTS
        // =========================================================

        var existingParkingCount =
            await context.ParkingSlots
                .CountAsync(x =>
                    x.EventId ==
                    eventEntity.Id);

        if (existingParkingCount == 0)
        {
            var parkingSlots =
                new List<ParkingSlot>();

            for (var number = 1;
                 number <= 20;
                 number++)
            {
                parkingSlots.Add(
                    new ParkingSlot
                    {
                        EventId =
                            eventEntity.Id,

                        SlotNumber =
                            $"A{number}",

                        Zone =
                            "A",

                        Fee =
                            500m,

                        Status =
                            ParkingSlotStatus
                                .Available
                    });
            }

            await context.ParkingSlots
                .AddRangeAsync(
                    parkingSlots);

            await context
                .SaveChangesAsync();
        }
    }
}