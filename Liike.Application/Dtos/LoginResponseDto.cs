namespace Liike.Application.Dtos;

public sealed class LoginResponseDto
{
    public required string AccessToken { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public required string Role { get; init; }

    public bool MustChangePassword { get; init; }
}