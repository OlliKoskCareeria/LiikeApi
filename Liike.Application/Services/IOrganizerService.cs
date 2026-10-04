using Liike.Domain;

namespace Liike.Application.Services;

public interface IOrganizerService
{
    Task<IReadOnlyList<Organizer>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Organizer> CreateByAdministratorAsync(
        Organizer organizer,
        CancellationToken cancellationToken = default);

    Task<Organizer?> GetByIdAsync(
        Guid organizerId,
        CancellationToken cancellationToken = default);

    Task<Organizer?> UpdateProfileAsync(
        Guid organizerId,
        Organizer profile,
        CancellationToken cancellationToken = default);

    Task<Organizer?> UpdateByAdministratorAsync(
        Guid organizerId,
        Organizer changes,
        CancellationToken cancellationToken = default);

    Task<bool> SetActiveAsync(
        Guid organizerId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteByAdministratorAsync(
        Guid organizerId,
        CancellationToken cancellationToken = default);
}
