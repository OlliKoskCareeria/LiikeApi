using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Liike.Application.Dtos;
using Liike.Application.Services;
using Liike.Domain;
using Liike.Infrastructure.Persistence;
using Liike.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Liike.Infrastructure.Services;

public sealed class AuthenticationService(
    ApplicationDbContext dbContext,
    IPasswordHasher<UserAccount> passwordHasher,
    IOptions<JwtOptions> jwtOptions) : IAuthenticationService
{
    public async Task<LoginResponseDto?> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var account = await dbContext.UserAccounts
            .Include(userAccount => userAccount.Organizer)
            .SingleOrDefaultAsync(
                userAccount => userAccount.NormalizedEmail == EmailNormalizer.Normalize(email),
                cancellationToken);

        if (account?.PasswordHash is null ||
            (account.Role == AccountRole.Organizer && account.Organizer?.IsActive != true))
        {
            return null;
        }

        var verification = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.PasswordHash = passwordHasher.HashPassword(account, password);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var options = jwtOptions.Value;
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(options.LifetimeMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, account.Email),
            new(ClaimTypes.Role, account.Role.ToString()),
            new("must_change_password", account.MustChangePassword ? "true" : "false"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (account.OrganizerId.HasValue)
        {
            claims.Add(new Claim("organizer_id", account.OrganizerId.Value.ToString()));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new LoginResponseDto
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt,
            Role = account.Role.ToString(),
            MustChangePassword = account.MustChangePassword
        };
    }

    public async Task<bool> ChangePasswordAsync(
        Guid accountId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var account = await dbContext.UserAccounts
            .SingleOrDefaultAsync(userAccount => userAccount.Id == accountId, cancellationToken);

        if (account?.PasswordHash is null || string.IsNullOrEmpty(currentPassword) ||
            string.IsNullOrEmpty(newPassword) ||
            passwordHasher.VerifyHashedPassword(account, account.PasswordHash, currentPassword) ==
            PasswordVerificationResult.Failed)
        {
            return false;
        }

        account.PasswordHash = passwordHasher.HashPassword(account, newPassword);
        account.MustChangePassword = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
