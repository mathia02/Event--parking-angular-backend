using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class BookingConfiguration
    : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BookingNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(x => x.BookingNumber)
            .IsUnique();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasDefaultValue(BookingStatus.Pending);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.CustomerId);

        builder.HasIndex(x => x.EventId);

        builder.HasIndex(x =>
            new
            {
                x.Status,
                x.HoldExpiresAt
            });

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Event)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.BookingSeats)
            .WithOne(x => x.Booking)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ParkingReservation)
            .WithOne(x => x.Booking)
            .HasForeignKey<ParkingReservation>(
                x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Payment)
            .WithOne(x => x.Booking)
            .HasForeignKey<Payment>(
                x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}