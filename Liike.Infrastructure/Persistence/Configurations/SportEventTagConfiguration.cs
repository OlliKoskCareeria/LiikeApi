using Liike.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Liike.Infrastructure.Persistence.Configurations;

public sealed class SportEventTagConfiguration : IEntityTypeConfiguration<SportEventTag>
{
    public void Configure(EntityTypeBuilder<SportEventTag> builder)
    {
        builder.ToTable("SportEventTags", table =>
            table.HasCheckConstraint("CK_SportEventTags_ValidTag", "\"Tag\" > 0"));

        builder.HasKey(tag => new { tag.SportEventId, tag.Tag });
        builder.Property(tag => tag.Tag)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(tag => tag.SportEvent)
            .WithMany(sportEvent => sportEvent.Tags)
            .HasForeignKey(tag => tag.SportEventId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(tag => new { tag.Tag, tag.SportEventId });
    }
}
