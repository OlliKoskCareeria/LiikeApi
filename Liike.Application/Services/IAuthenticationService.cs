using Liike.Application.Dtos;

namespace Liike.Application.Services;

public interface IAuthenticationService
{
    Task<LoginResponseDto?> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<bool> ChangePasswordAsync(
        Guid accountId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
}