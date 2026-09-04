using EventParking.DataAccess.Context;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context)
    {
        // ---------------------------------------------------------
        // APPLY PENDING MIGRATIONS
        // ---------------------------------------------------------

        if ((await context.Database
                .GetPendingMigrationsAsync())
            .Any())
        {
            await context.Database
                .MigrateAsync();
        }

        // ---------------------------------------------------------
        // ADMIN
        // ---------------------------------------------------------

        var adminEmail =
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
                        BCrypt.Net.BCrypt.HashPassword(
                            "Admin@123"),

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

            await context
                .SaveChangesAsync();
        }

        // ---------------------------------------------------------
        // DEFAULT CUSTOMER
        // ---------------------------------------------------------

        var customerEmail =
            "customer1@test.com";

        var customer =
            await context.Customers
                .FirstOrDefaultAsync(x =>
                    x.Email == customerEmail);

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
                        BCrypt.Net.BCrypt.HashPassword(
                            "Customer@123"),

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

            await context
                .SaveChangesAsync();
        }

        // ---------------------------------------------------------
        // VENUE
        // ---------------------------------------------------------

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

        // ---------------------------------------------------------
        // CATEGORY
        // ---------------------------------------------------------

        const string categoryName =
            "Technology";

        var category =
            await context.EventCategories
                .FirstOrDefaultAsync(x =>
                    x.Name == categoryName);

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

        // ---------------------------------------------------------
        // EVENT
        // ---------------------------------------------------------

        const string eventName =
            "Tech Conference 2026";

        var eventEntity =
            await context.Events
                .FirstOrDefaultAsync(x =>
                    x.Name == eventName);

        if (eventEntity == null)
        {
            eventEntity =
                new Event
                {
                    Name =
                        eventName,

                    Description =
                        "Annual technology conference.",

                    VenueId =
                        venue.Id,

                    CategoryId =
                        category.Id,

                    StartDateTime =
                        new DateTime(
                            2026,
                            9,
                            10,
                            10,
                            0,
                            0),

                    EndDateTime =
                        new DateTime(
                            2026,
                            9,
                            10,
                            12,
                            0,
                            0),

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

        // ---------------------------------------------------------
        // SEATS
        // 15 ROWS x 20 SEATS = 300
        // ---------------------------------------------------------

        var existingSeatCount =
            await context.Seats
                .CountAsync(x =>
                    x.EventId ==
                    eventEntity.Id);

        if (existingSeatCount == 0)
        {
            var seats =
                new List<Seat>();

            const int rows =
                15;

            const int columns =
                20;

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
                                SeatStatus
                                    .Available
                        });
                }
            }

            await context.Seats
                .AddRangeAsync(seats);

            await context
                .SaveChangesAsync();
        }

        // ---------------------------------------------------------
        // PARKING SLOTS
        // ---------------------------------------------------------

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