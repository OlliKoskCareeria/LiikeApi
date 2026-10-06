namespace Liike.Domain;

public sealed class UserAccount
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public AccountRole Role { get; set; }

    public Guid? OrganizerId { get; set; }

    public Organizer? Organizer { get; set; }

    public bool MustChangePassword { get; set; }
}