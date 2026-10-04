namespace Liike.Domain;

public class Organizer
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public OrganizerType Type { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Website { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<SportEvent> Events { get; set; } = [];
}
