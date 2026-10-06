using System.Security.Claims;
using Liike.Application.Dtos;
using Liike.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Liike.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthenticationController(IAuthenticationService authenticationService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("Login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await authenticationService.LoginAsync(
            request.Email,
            request.Password,
            cancellationToken);

        return response is null ? Unauthorized() : Ok(response);
    }

    [HttpPost("change-password")]
    [Authorize(Policy = "AccountOwner")]
    [EnableRateLimiting("Organizer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto request,
        CancellationToken cancellationToken)
    {
        var accountIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(accountIdValue, out var accountId))
        {
            return Forbid();
        }

        var changed = await authenticationService.ChangePasswordAsync(
            accountId,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        return changed ? NoContent() : BadRequest();
    }
}
