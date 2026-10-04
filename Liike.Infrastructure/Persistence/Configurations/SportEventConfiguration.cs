using Liike.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Liike.Infrastructure.Persistence.Configurations;

public sealed class SportEventConfiguration : IEntityTypeConfiguration<SportEvent>
{
    public void Configure(EntityTypeBuilder<SportEvent> builder)
    {
        builder.ToTable("SportEvents", table =>
        {
            table.HasCheckConstraint(
                "CK_SportEvents_TimeRange",
                "\"EndTime\" > \"StartTime\"");
            table.HasCheckConstraint(
                "CK_SportEvents_Coordinates",
                "((\"Latitude\" IS NULL AND \"Longitude\" IS NULL) OR " +
                "(\"Latitude\" IS NOT NULL AND \"Longitude\" IS NOT NULL)) AND " +
                "(\"Latitude\" IS NULL OR \"Latitude\" BETWEEN -90 AND 90) AND " +
                "(\"Longitude\" IS NULL OR \"Longitude\" BETWEEN -180 AND 180)");
            table.HasCheckConstraint(
                "CK_SportEvents_NonNegativePrice",
                "\"Price\" IS NULL OR \"Price\" >= 0");
        });

        builder.HasKey(sportEvent => sportEvent.Id);

        builder.Property(sportEvent => sportEvent.Title)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(sportEvent => sportEvent.Description)
            .HasColumnType("text")
            .IsRequired();
        builder.Property(sportEvent => sportEvent.Sport)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(sportEvent => sportEvent.Category)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(sportEvent => sportEvent.StartTime)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(sportEvent => sportEvent.EndTime)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(sportEvent => sportEvent.Municipality)
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(sportEvent => sportEvent.Address)
            .HasMaxLength(300)
            .IsRequired();
        builder.Property(sportEvent => sportEvent.Latitude)
            .IsRequired(false);
        builder.Property(sportEvent => sportEvent.Longitude)
            .IsRequired(false);
        builder.Property(sportEvent => sportEvent.Price)
            .HasPrecision(12, 2)
            .IsRequired(false);
        builder.Property(sportEvent => sportEvent.PriceDescription)
            .HasColumnType("text")
            .IsRequired(false);
        builder.Property(sportEvent => sportEvent.RegistrationInfo)
            .HasColumnType("text")
            .IsRequired(false);
        builder.Property(sportEvent => sportEvent.EquipmentInfo)
            .HasColumnType("text")
            .IsRequired(false);
        builder.Property(sportEvent => sportEvent.ArrivalInfo)
            .HasColumnType("text")
            .IsRequired(false);
        builder.Property(sportEvent => sportEvent.AdditionalInfo)
            .HasColumnType("text")
            .IsRequired(false);

        builder.HasIndex(sportEvent => new { sportEvent.IsPublished, sportEvent.StartTime });
        builder.HasIndex(sportEvent => new
        {
            sportEvent.IsPublished,
            sportEvent.Municipality,
            sportEvent.StartTime
        });
        builder.HasIndex(sportEvent => new
        {
            sportEvent.IsPublished,
            sportEvent.Category,
            sportEvent.StartTime
        });
    }
}
