using Liike.Domain;

namespace Liike.Application.Dtos;

public sealed class SportEventDto
{
    public Guid Id { get; init; }

    public Guid OrganizerId { get; init; }

    public string OrganizerName { get; init; } = string.Empty;

    public OrganizerType OrganizerType { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Sport { get; init; } = string.Empty;

    public SportCategory Category { get; init; }

    public IReadOnlyList<EventTag> Tags { get; init; } = [];

    public DateTimeOffset StartTime { get; init; }

    public DateTimeOffset EndTime { get; init; }

    public bool IsRecurring { get; init; }

    public bool IsPublished { get; init; }

    public string Municipality { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public decimal? Price { get; init; }

    public string? PriceDescription { get; init; }

    public string? RegistrationInfo { get; init; }

    public string? EquipmentInfo { get; init; }

    public string? ArrivalInfo { get; init; }

    public string? AdditionalInfo { get; init; }
}
