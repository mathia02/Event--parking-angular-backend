using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class BookingSeatConfiguration
    : IEntityTypeConfiguration<BookingSeat>
{
    public void Configure(
        EntityTypeBuilder<BookingSeat> builder)
    {
        builder.ToTable("BookingSeats");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PriceAtBooking)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.ReservedAt)
            .IsRequired();

        builder.HasIndex(x =>
            new
            {
                x.BookingId,
                x.SeatId
            })
            .IsUnique();

        // Only one ACTIVE claim can exist for a seat.
        builder.HasIndex(x => x.SeatId)
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.HasOne(x => x.Booking)
            .WithMany(x => x.BookingSeats)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Seat)
            .WithMany(x => x.BookingSeats)
            .HasForeignKey(x => x.SeatId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}