using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventParking.DataAccess.Configurations;

public class VenueConfiguration
    : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable(
            "Venues",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_Venues_Capacity",
                    "[Capacity] > 0");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Address)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Capacity)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.Name);

        builder.HasMany(x => x.Events)
            .WithOne(x => x.Venue)
            .HasForeignKey(x => x.VenueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}