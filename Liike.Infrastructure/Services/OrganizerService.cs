using Liike.Application.Services;
using Liike.Domain;
using Liike.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Liike.Infrastructure.Services;

public sealed class OrganizerService(ApplicationDbContext dbContext) : IOrganizerService
{
    public async Task<IReadOnlyList<Organizer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Organizers
            .AsNoTracking()
            .OrderBy(organizer => organizer.Name)
            .ThenBy(organizer => organizer.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Organizer> CreateByAdministratorAsync(
        Organizer organizer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizer);

        var entity = new Organizer
        {
            Name = organizer.Name,
            Type = organizer.Type,
            Email = organizer.Email,
            PhoneNumber = organizer.PhoneNumber,
            Website = organizer.Website,
            IsActive = true
        };

        dbContext.Organizers.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public Task<Organizer?> GetByIdAsync(
        Guid organizerId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Organizers
            .AsNoTracking()
            .SingleOrDefaultAsync(organizer => organizer.Id == organizerId, cancellationToken);
    }

    public async Task<Organizer?> UpdateProfileAsync(
        Guid organizerId,
        Organizer profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var entity = await dbContext.Organizers
            .SingleOrDefaultAsync(organizer => organizer.Id == organizerId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Name = profile.Name;
        entity.Email = profile.Email;
        entity.PhoneNumber = profile.PhoneNumber;
        entity.Website = profile.Website;

        await dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Organizer?> UpdateByAdministratorAsync(
        Guid organizerId,
        Organizer changes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(changes);

        var entity = await dbContext.Organizers
            .SingleOrDefaultAsync(organizer => organizer.Id == organizerId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Name = changes.Name;
        entity.Type = changes.Type;
        entity.Email = changes.Email;
        entity.PhoneNumber = changes.PhoneNumber;
        entity.Website = changes.Website;
        entity.IsActive = changes.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<bool> SetActiveAsync(
        Guid organizerId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Organizers
            .SingleOrDefaultAsync(organizer => organizer.Id == organizerId, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteByAdministratorAsync(
        Guid organizerId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Organizers
            .SingleOrDefaultAsync(organizer => organizer.Id == organizerId, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        if (await dbContext.SportEvents.AnyAsync(
                sportEvent => sportEvent.OrganizerId == organizerId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "An organizer with existing events cannot be deleted.");
        }

        dbContext.Organizers.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
