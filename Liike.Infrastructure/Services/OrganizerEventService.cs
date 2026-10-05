using Liike.Application.Services;
using Liike.Domain;
using Liike.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Liike.Infrastructure.Services;

public sealed class OrganizerEventService(
    ApplicationDbContext dbContext,
    IEnumerable<IGeocodingService> geocodingServices) : IOrganizerEventService
{
    private readonly IGeocodingService[] _geocodingServices = geocodingServices.ToArray();

    public async Task<IReadOnlyList<SportEvent>> GetOwnEventsAsync(
        Guid organizerId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SportEvents
            .AsNoTracking()
            .Where(sportEvent => sportEvent.OrganizerId == organizerId)
            .Include(sportEvent => sportEvent.Tags)
            .OrderBy(sportEvent => sportEvent.StartTime)
            .ThenBy(sportEvent => sportEvent.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<SportEvent> CreateAsync(
        Guid organizerId,
        SportEvent eventToCreate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventToCreate);
        ValidateEvent(eventToCreate);

        var organizer = await dbContext.Organizers
            .SingleOrDefaultAsync(item => item.Id == organizerId, cancellationToken)
            ?? throw new KeyNotFoundException("The organizer was not found.");

        if (!organizer.IsActive)
        {
            throw new InvalidOperationException("An inactive organizer cannot create events.");
        }

        var coordinates = await GeocodeAsync(eventToCreate, cancellationToken);
        var entity = new SportEvent
        {
            OrganizerId = organizerId,
            Organizer = organizer,
            IsPublished = true
        };

        CopyEditableFields(entity, eventToCreate);
        entity.Latitude = coordinates.Latitude;
        entity.Longitude = coordinates.Longitude;
        entity.Tags = CreateTags(eventToCreate.Tags);

        dbContext.SportEvents.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<SportEvent?> UpdateAsync(
        Guid organizerId,
        Guid eventId,
        SportEvent changes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(changes);
        ValidateEvent(changes);

        var entity = await dbContext.SportEvents
            .Include(sportEvent => sportEvent.Organizer)
            .Include(sportEvent => sportEvent.Tags)
            .SingleOrDefaultAsync(
                sportEvent => sportEvent.Id == eventId && sportEvent.OrganizerId == organizerId,
                cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (!entity.Organizer.IsActive)
        {
            throw new InvalidOperationException("An inactive organizer cannot update events.");
        }

        var locationChanged =
            !string.Equals(entity.Municipality, changes.Municipality, StringComparison.Ordinal) ||
            !string.Equals(entity.Address, changes.Address, StringComparison.Ordinal);

        if (locationChanged)
        {
            var coordinates = await GeocodeAsync(changes, cancellationToken);
            entity.Latitude = coordinates.Latitude;
            entity.Longitude = coordinates.Longitude;
        }

        CopyEditableFields(entity, changes);

        dbContext.SportEventTags.RemoveRange(entity.Tags);
        entity.Tags = CreateTags(changes.Tags);

        await dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<bool> DeleteAsync(
        Guid organizerId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.SportEvents
            .SingleOrDefaultAsync(
                sportEvent => sportEvent.Id == eventId && sportEvent.OrganizerId == organizerId,
                cancellationToken);
        if (entity is null)
        {
            return false;
        }

        dbContext.SportEvents.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<(double Latitude, double Longitude)> GeocodeAsync(
        SportEvent sportEvent,
        CancellationToken cancellationToken)
    {
        if (_geocodingServices.Length == 0)
        {
            throw new GeocodingProviderUnavailableException();
        }

        if (_geocodingServices.Length > 1)
        {
            throw new InvalidOperationException("Only one geocoding provider may be registered.");
        }

        var coordinates = await _geocodingServices[0].GeocodeAsync(
            sportEvent.Address,
            sportEvent.Municipality,
            cancellationToken);

        if (coordinates is null ||
            !double.IsFinite(coordinates.Value.Latitude) ||
            !double.IsFinite(coordinates.Value.Longitude) ||
            coordinates.Value.Latitude is < -90 or > 90 ||
            coordinates.Value.Longitude is < -180 or > 180)
        {
            throw new AddressCouldNotBeGeocodedException();
        }

        return coordinates.Value;
    }

    private static void ValidateEvent(SportEvent sportEvent)
    {
        if (string.IsNullOrWhiteSpace(sportEvent.Title) ||
            string.IsNullOrWhiteSpace(sportEvent.Description) ||
            string.IsNullOrWhiteSpace(sportEvent.Sport) ||
            string.IsNullOrWhiteSpace(sportEvent.Municipality) ||
            string.IsNullOrWhiteSpace(sportEvent.Address))
        {
            throw new ArgumentException("Required event fields must not be empty.");
        }

        if (sportEvent.Category == SportCategory.Unspecified ||
            !Enum.IsDefined(typeof(SportCategory), sportEvent.Category))
        {
            throw new ArgumentException("A valid sport category is required.");
        }

        if (sportEvent.StartTime >= sportEvent.EndTime)
        {
            throw new ArgumentException("The event end time must be after its start time.");
        }

        if (sportEvent.Price < 0)
        {
            throw new ArgumentException("Event price cannot be negative.");
        }

        if (sportEvent.Tags.Any(tag =>
                tag.Tag == EventTag.Unspecified || !Enum.IsDefined(typeof(EventTag), tag.Tag)))
        {
            throw new ArgumentException("Event tags must contain valid values.");
        }
    }

    private static void CopyEditableFields(SportEvent target, SportEvent source)
    {
        target.Title = source.Title;
        target.Description = source.Description;
        target.Sport = source.Sport;
        target.Category = source.Category;
        target.StartTime = source.StartTime;
        target.EndTime = source.EndTime;
        target.IsRecurring = source.IsRecurring;
        target.Municipality = source.Municipality;
        target.Address = source.Address;
        target.Price = source.Price;
        target.PriceDescription = source.PriceDescription;
        target.RegistrationInfo = source.RegistrationInfo;
        target.EquipmentInfo = source.EquipmentInfo;
        target.ArrivalInfo = source.ArrivalInfo;
        target.AdditionalInfo = source.AdditionalInfo;
    }

    private static List<SportEventTag> CreateTags(IEnumerable<SportEventTag> tags)
    {
        return tags
            .Select(tag => tag.Tag)
            .Distinct()
            .Select(tag => new SportEventTag { Tag = tag })
            .ToList();
    }
}
