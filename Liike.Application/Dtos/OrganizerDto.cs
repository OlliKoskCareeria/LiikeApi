using Liike.Domain;

namespace Liike.Application.Dtos;

public sealed class OrganizerDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public OrganizerType Type { get; init; }

    public string Email { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public string? Website { get; init; }

    public bool IsActive { get; init; }
}
