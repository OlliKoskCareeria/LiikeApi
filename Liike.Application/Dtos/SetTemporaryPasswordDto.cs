using System.ComponentModel.DataAnnotations;

namespace Liike.Application.Dtos;

public sealed class SetTemporaryPasswordDto
{
    [Required]
    [StringLength(128, MinimumLength = 12)]
    public required string TemporaryPassword { get; init; }
}