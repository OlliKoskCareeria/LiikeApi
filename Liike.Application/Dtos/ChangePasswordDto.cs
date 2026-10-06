using System.ComponentModel.DataAnnotations;

namespace Liike.Application.Dtos;

public sealed class ChangePasswordDto
{
    [Required]
    [StringLength(128, MinimumLength = 12)]
    public required string CurrentPassword { get; init; }

    [Required]
    [StringLength(128, MinimumLength = 12)]
    public required string NewPassword { get; init; }
}