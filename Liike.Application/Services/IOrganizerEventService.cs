using Liike.Domain;

namespace Liike.Application.Services;

public interface IOrganizerEventService
{
    Task<IReadOnlyList<SportEvent>> GetOwnEventsAsync(
        Guid organizerId,
        CancellationToken cancellationToken = default);

    Task<SportEvent> CreateAsync(
        Guid organizerId,
        SportEvent eventToCreate,
        CancellationToken cancellationToken = default);

    Task<SportEvent?> UpdateAsync(
        Guid organizerId,
        Guid eventId,
        SportEvent changes,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid organizerId,
        Guid eventId,
        CancellationToken cancellationToken = default);
}
