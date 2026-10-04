namespace Liike.Domain;

public class SportEventTag
{
    public Guid SportEventId { get; set; }

    public SportEvent SportEvent { get; set; } = null!;

    public EventTag Tag { get; set; }
}
