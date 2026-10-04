namespace Liike.Domain;

public class SportEvent
{
    private DateTimeOffset _startTime;
    private DateTimeOffset _endTime;

    public Guid Id { get; set; }

    public Guid OrganizerId { get; set; }

    public Organizer Organizer { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Sport { get; set; } = string.Empty;

    public SportCategory Category { get; set; }

    public ICollection<SportEventTag> Tags { get; set; } = [];

    // Store event instants in UTC; retain a separate time-zone identifier if local recurrence is later required.
    public DateTimeOffset StartTime
    {
        get => _startTime;
        set => _startTime = value.ToUniversalTime();
    }

    public DateTimeOffset EndTime
    {
        get => _endTime;
        set => _endTime = value.ToUniversalTime();
    }

    public bool IsRecurring { get; set; }

    public bool IsPublished { get; set; } = true;

    public string Municipality { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public decimal? Price { get; set; }

    public string? PriceDescription { get; set; }

    public string? RegistrationInfo { get; set; }

    public string? EquipmentInfo { get; set; }

    public string? ArrivalInfo { get; set; }

    public string? AdditionalInfo { get; set; }
}
