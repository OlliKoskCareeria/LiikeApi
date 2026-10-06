using Liike.Application.Services;
using Liike.Domain;
using Liike.Infrastructure.Persistence;
using Liike.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Liike.Infrastructure.Services;

public sealed class OrganizerService(
    ApplicationDbContext dbContext,
    IPasswordHasher<UserAccount> passwordHasher) : IOrganizerService
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
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organizer);
        ValidateTemporaryPassword(temporaryPassword);

        var email = EmailNormalizer.Clean(organizer.Email);
        var normalizedEmail = EmailNormalizer.Normalize(email);
        if (await dbContext.UserAccounts.AnyAsync(
                account => account.NormalizedEmail == normalizedEmail,
                cancellationToken))
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }

        var entity = new Organizer
        {
            Name = organizer.Name,
            Type = organizer.Type,
            Email = email,
            PhoneNumber = organizer.PhoneNumber,
            Website = organizer.Website,
            IsActive = true
        };

        dbContext.Organizers.Add(entity);
        var account = new UserAccount
        {
            Email = email,
            NormalizedEmail = normalizedEmail,
            Role = AccountRole.Organizer,
            Organizer = entity,
            MustChangePassword = true
        };
        account.PasswordHash = passwordHasher.HashPassword(account, temporaryPassword);
        dbContext.UserAccounts.Add(account);
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

        var email = EmailNormalizer.Clean(profile.Email);
        var normalizedEmail = EmailNormalizer.Normalize(email);
        var account = await dbContext.UserAccounts
            .SingleOrDefaultAsync(userAccount => userAccount.OrganizerId == organizerId, cancellationToken);
        if (await dbContext.UserAccounts.AnyAsync(
                userAccount => userAccount.NormalizedEmail == normalizedEmail &&
                               userAccount.OrganizerId != organizerId,
                cancellationToken))
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }

        entity.Name = profile.Name;
        entity.Email = email;
        entity.PhoneNumber = profile.PhoneNumber;
        entity.Website = profile.Website;
        if (account is not null)
        {
            account.Email = email;
            account.NormalizedEmail = normalizedEmail;
        }

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

    public async Task<bool> SetTemporaryPasswordAsync(
        Guid organizerId,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        ValidateTemporaryPassword(temporaryPassword);

        var account = await dbContext.UserAccounts
            .SingleOrDefaultAsync(userAccount => userAccount.OrganizerId == organizerId, cancellationToken);
        if (account is null)
        {
            return false;
        }

        account.PasswordHash = passwordHasher.HashPassword(account, temporaryPassword);
        account.MustChangePassword = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int?> GetEventCountAsync(
        Guid organizerId,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Organizers.AnyAsync(organizer => organizer.Id == organizerId, cancellationToken))
        {
            return null;
        }

        return await dbContext.SportEvents.CountAsync(
            sportEvent => sportEvent.OrganizerId == organizerId,
            cancellationToken);
    }

    public async Task<bool> DeleteOwnAccountAsync(
        Guid organizerId,
        int expectedEventCount,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var entity = await dbContext.Organizers
            .FromSqlInterpolated($"SELECT * FROM \"Organizers\" WHERE \"Id\" = {organizerId} FOR UPDATE")
            .SingleOrDefaultAsync(organizer => organizer.Id == organizerId, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        var eventCount = await dbContext.SportEvents.CountAsync(
            sportEvent => sportEvent.OrganizerId == organizerId,
            cancellationToken);
        if (eventCount != expectedEventCount)
        {
            return false;
        }

        dbContext.Organizers.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static void ValidateTemporaryPassword(string temporaryPassword)
    {
        if (string.IsNullOrWhiteSpace(temporaryPassword) || temporaryPassword.Length is < 12 or > 128)
        {
            throw new ArgumentException(
                "The temporary password must be between 12 and 128 characters.",
                nameof(temporaryPassword));
        }
    }
}
