using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class ParkingReservationConfiguration
    : IEntityTypeConfiguration<ParkingReservation>
{
    public void Configure(
        EntityTypeBuilder<ParkingReservation> builder)
    {
        builder.ToTable("ParkingReservations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReservedFee)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.ReservedAt)
            .IsRequired();

        // One parking reservation record per booking.
        builder.HasIndex(x => x.BookingId)
            .IsUnique();

        // One parking slot cannot have two active reservations.
        builder.HasIndex(x => x.ParkingSlotId)
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.HasOne(x => x.Booking)
            .WithOne(x => x.ParkingReservation)
            .HasForeignKey<ParkingReservation>(
                x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ParkingSlot)
            .WithMany(x => x.ParkingReservations)
            .HasForeignKey(x => x.ParkingSlotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}