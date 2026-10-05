using Liike.Domain;

namespace Liike.Application.Services;

public interface IPublicEventService
{
    Task<IReadOnlyList<(SportEvent Event, double DistanceKilometers)>> SearchNearbyPublishedAsync(
        double latitude,
        double longitude,
        double? radiusKilometers = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SportEvent>> SearchPublishedAsync(
        string? municipality = null,
        SportCategory? category = null,
        decimal? minimumPrice = null,
        decimal? maximumPrice = null,
        OrganizerType? organizerType = null,
        EventTag? tag = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<SportEvent?> GetPublishedByIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
}
