using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class ParkingSlotConfiguration
    : IEntityTypeConfiguration<ParkingSlot>
{
    public void Configure(
        EntityTypeBuilder<ParkingSlot> builder)
    {
        builder.ToTable("ParkingSlots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SlotNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Zone)
            .HasMaxLength(50);

        builder.Property(x => x.Fee)
            .HasPrecision(18, 2);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasDefaultValue(ParkingSlotStatus.Available);

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x =>
            new
            {
                x.EventId,
                x.SlotNumber
            })
            .IsUnique();

        builder.HasOne(x => x.Event)
            .WithMany(x => x.ParkingSlots)
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}