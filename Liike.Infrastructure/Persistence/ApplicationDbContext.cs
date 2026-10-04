using Liike.Domain;
using Microsoft.EntityFrameworkCore;

namespace Liike.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Organizer> Organizers => Set<Organizer>();

    public DbSet<SportEvent> SportEvents => Set<SportEvent>();

    public DbSet<SportEventTag> SportEventTags => Set<SportEventTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
