using Liike.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Liike.Infrastructure.Persistence.Configurations;

public sealed class OrganizerConfiguration : IEntityTypeConfiguration<Organizer>
{
    public void Configure(EntityTypeBuilder<Organizer> builder)
    {
        builder.ToTable("Organizers");
        builder.HasKey(organizer => organizer.Id);

        builder.Property(organizer => organizer.Name)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(organizer => organizer.Email)
            .HasMaxLength(320)
            .IsRequired();
        builder.Property(organizer => organizer.Type)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(organizer => organizer.PhoneNumber)
            .HasMaxLength(32)
            .IsRequired(false);
        builder.Property(organizer => organizer.Website)
            .HasMaxLength(2048)
            .IsRequired(false);

        builder.HasIndex(organizer => organizer.Email);

        builder.HasMany(organizer => organizer.Events)
            .WithOne(sportEvent => sportEvent.Organizer)
            .HasForeignKey(sportEvent => sportEvent.OrganizerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
