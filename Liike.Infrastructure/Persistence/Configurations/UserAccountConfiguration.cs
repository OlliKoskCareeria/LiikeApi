using Liike.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Liike.Infrastructure.Persistence.Configurations;

public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("UserAccounts");
        builder.HasKey(account => account.Id);

        builder.Property(account => account.Email)
            .HasMaxLength(320)
            .IsRequired();
        builder.Property(account => account.NormalizedEmail)
            .HasMaxLength(320)
            .IsRequired();
        builder.Property(account => account.PasswordHash)
            .HasMaxLength(512)
            .IsRequired(false);
        builder.Property(account => account.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(account => account.NormalizedEmail)
            .IsUnique();
        builder.HasIndex(account => account.OrganizerId)
            .IsUnique();

        builder.HasOne(account => account.Organizer)
            .WithOne()
            .HasForeignKey<UserAccount>(account => account.OrganizerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}