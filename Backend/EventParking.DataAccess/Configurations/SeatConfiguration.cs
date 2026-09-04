using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class SeatConfiguration
    : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.ToTable("Seats");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SeatNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.RowLabel)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.ColumnNumber)
            .IsRequired();

        builder.Property(x => x.SeatType)
            .HasMaxLength(50);

        builder.Property(x => x.PriceOverride)
            .HasPrecision(18, 2);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasDefaultValue(SeatStatus.Available);

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x =>
            new
            {
                x.EventId,
                x.SeatNumber
            })
            .IsUnique();

        builder.HasOne(x => x.Event)
            .WithMany(x => x.Seats)
            .HasForeignKey(x => x.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}