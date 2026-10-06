using System.ComponentModel.DataAnnotations;

namespace Liike.Application.Dtos;

public sealed class LoginRequestDto
{
    [Required]
    [EmailAddress]
    [StringLength(320)]
    public required string Email { get; init; }

    [Required]
    [StringLength(128, MinimumLength = 12)]
    public required string Password { get; init; }
}