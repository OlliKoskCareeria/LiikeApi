using System.ComponentModel.DataAnnotations;

namespace Liike.Application.Dtos;

public sealed class UpdateOrganizerDto
{
    [Required]
    [StringLength(200)]
    public required string Name { get; init; }

    [Required]
    [EmailAddress]
    [StringLength(320)]
    public required string Email { get; init; }

    [StringLength(32)]
    public string? PhoneNumber { get; init; }

    [StringLength(2048)]
    public string? Website { get; init; }
}
