using Liike.Application.Services;
using Liike.Domain;
using Liike.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Liike.Infrastructure.Services;

public sealed class PublicEventService(ApplicationDbContext dbContext) : IPublicEventService
{
    public async Task<IReadOnlyList<(SportEvent Event, double DistanceKilometers)>> SearchNearbyPublishedAsync(
        double latitude,
        double longitude,
        double? radiusKilometers = null,
        CancellationToken cancellationToken = default)
    {
        ValidateCoordinates(latitude, longitude);

        if (radiusKilometers.HasValue &&
            (!double.IsFinite(radiusKilometers.Value) || radiusKilometers.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(radiusKilometers));
        }

        const double earthRadiusKilometers = 6371d;
        var originLatitudeRadians = ToRadians(latitude);
        var originLongitudeRadians = ToRadians(longitude);

        var query = dbContext.SportEvents
            .AsNoTracking()
            .Where(sportEvent => sportEvent.IsPublished &&
                                 sportEvent.Latitude.HasValue &&
                                 sportEvent.Longitude.HasValue)
            .Include(sportEvent => sportEvent.Organizer)
            .Include(sportEvent => sportEvent.Tags)
            .Select(sportEvent => new
            {
                Event = sportEvent,
                DistanceKilometers = earthRadiusKilometers * 2d * Math.Asin(
                    Math.Sqrt(
                        Math.Min(
                            1d,
                            Math.Max(
                                0d,
                                Math.Pow(Math.Sin((sportEvent.Latitude!.Value * Math.PI / 180d - originLatitudeRadians) / 2d), 2d) +
                                Math.Cos(originLatitudeRadians) *
                                Math.Cos(sportEvent.Latitude.Value * Math.PI / 180d) *
                                Math.Pow(Math.Sin((sportEvent.Longitude!.Value * Math.PI / 180d - originLongitudeRadians) / 2d), 2d)))))
            });

        if (radiusKilometers.HasValue)
        {
            query = query.Where(result => result.DistanceKilometers <= radiusKilometers.Value);
        }

        var results = await query
            .OrderBy(result => result.DistanceKilometers)
            .ThenBy(result => result.Event.Id)
            .ToListAsync(cancellationToken);

        return results
            .Select(result => (result.Event, result.DistanceKilometers))
            .ToList();
    }

    public async Task<IReadOnlyList<SportEvent>> SearchPublishedAsync(
        string? municipality = null,
        SportCategory? category = null,
        decimal? minimumPrice = null,
        decimal? maximumPrice = null,
        OrganizerType? organizerType = null,
        EventTag? tag = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        if (pageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 100.");
        }

        if (minimumPrice < 0 || maximumPrice < 0 ||
            (minimumPrice.HasValue && maximumPrice.HasValue && minimumPrice > maximumPrice))
        {
            throw new ArgumentException("The price range is invalid.");
        }

        var query = dbContext.SportEvents
            .AsNoTracking()
            .Where(sportEvent => sportEvent.IsPublished)
            .Include(sportEvent => sportEvent.Organizer)
            .Include(sportEvent => sportEvent.Tags)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(municipality))
        {
            var location = municipality.Trim();
            query = query.Where(sportEvent => sportEvent.Municipality == location);
        }

        if (category.HasValue)
        {
            query = query.Where(sportEvent => sportEvent.Category == category.Value);
        }

        if (minimumPrice.HasValue)
        {
            query = query.Where(sportEvent =>
                sportEvent.Price.HasValue && sportEvent.Price.Value >= minimumPrice.Value);
        }

        if (maximumPrice.HasValue)
        {
            query = query.Where(sportEvent =>
                sportEvent.Price.HasValue && sportEvent.Price.Value <= maximumPrice.Value);
        }

        if (organizerType.HasValue)
        {
            query = query.Where(sportEvent => sportEvent.Organizer.Type == organizerType.Value);
        }

        if (tag.HasValue)
        {
            query = query.Where(sportEvent => sportEvent.Tags.Any(eventTag => eventTag.Tag == tag.Value));
        }

        var skip = checked((pageNumber - 1) * pageSize);
        return await query
            .OrderBy(sportEvent => sportEvent.StartTime)
            .ThenBy(sportEvent => sportEvent.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<SportEvent?> GetPublishedByIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.SportEvents
            .AsNoTracking()
            .Where(sportEvent => sportEvent.Id == eventId && sportEvent.IsPublished)
            .Include(sportEvent => sportEvent.Organizer)
            .Include(sportEvent => sportEvent.Tags)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static void ValidateCoordinates(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude));
        }

        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude));
        }
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;
}
