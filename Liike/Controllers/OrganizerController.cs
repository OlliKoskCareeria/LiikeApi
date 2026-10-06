using Liike.Application.Dtos;
using Liike.Application.Services;
using Liike.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Liike.Controllers;

/// <summary>
/// Provides admin organizer-management and authenticated organizer self-service endpoints.
/// </summary>
[ApiController]
[Route("api/organizers")]
[Produces("application/json")]
public sealed class OrganizerController(IOrganizerService organizerService) : ControllerBase
{
    /// <summary>Gets all organizers, including inactive organizers.</summary>
    [HttpGet]
    [Authorize(Policy = "Admin")]
    [EnableRateLimiting("Organizer")]
    [ProducesResponseType(typeof(IReadOnlyList<OrganizerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrganizerDto>>> GetAll(CancellationToken cancellationToken)
    {
        var organizers = await organizerService.GetAllAsync(cancellationToken);
        return Ok(organizers.Select(ToDto).ToList());
    }

    /// <summary>Gets an organizer by its identifier.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Admin")]
    [EnableRateLimiting("Organizer")]
    [ProducesResponseType(typeof(OrganizerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrganizerDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        var organizer = await organizerService.GetByIdAsync(id, cancellationToken);
        return organizer is null ? NotFound() : Ok(ToDto(organizer));
    }

    /// <summary>Creates an organizer record.</summary>
    [HttpPost]
    [Authorize(Policy = "Admin")]
    [EnableRateLimiting("Organizer")]
    [ProducesResponseType(typeof(OrganizerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrganizerDto>> Create(
        [FromBody] CreateOrganizerDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var organizer = await organizerService.CreateByAdministratorAsync(
                new Organizer
                {
                    Name = request.Name,
                    Type = request.Type,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Website = request.Website
                },
                request.TemporaryPassword,
                cancellationToken);

            var response = ToDto(organizer);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
        catch (InvalidOperationException)
        {
            return Conflict();
        }
    }

    /// <summary>Replaces an organizer's editable profile fields.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin")]
    [EnableRateLimiting("Organizer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateOrganizerDto request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        try
        {
            var organizer = await organizerService.UpdateProfileAsync(
                id,
                new Organizer
                {
                    Name = request.Name,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Website = request.Website
                },
                cancellationToken);

            return organizer is null ? NotFound() : NoContent();
        }
        catch (InvalidOperationException)
        {
            return Conflict();
        }
    }

    /// <summary>Deletes an organizer that has no associated events.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Admin")]
    [EnableRateLimiting("Organizer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        try
        {
            return await organizerService.DeleteByAdministratorAsync(id, cancellationToken)
                ? NoContent()
                : NotFound();
        }
        catch (InvalidOperationException)
        {
            return Conflict();
        }
    }

    [HttpPatch("{id:guid}/active")]
    [Authorize(Policy = "Admin")]
    [EnableRateLimiting("Organizer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetActive(
        Guid id,
        [FromBody] SetOrganizerActiveDto request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        return await organizerService.SetActiveAsync(id, request.IsActive, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpPost("{id:guid}/temporary-password")]
    [Authorize(Policy = "Admin")]
    [EnableRateLimiting("Organizer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetTemporaryPassword(
        Guid id,
        [FromBody] SetTemporaryPasswordDto request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        try
        {
            return await organizerService.SetTemporaryPasswordAsync(
                id,
                request.TemporaryPassword,
                cancellationToken)
                ? NoContent()
                : NotFound();
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    [HttpGet("me")]
    [Authorize(Policy = "Organizer")]
    [EnableRateLimiting("Organizer")]
    [ProducesResponseType(typeof(OrganizerDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizerDto>> GetMyProfile(CancellationToken cancellationToken)
    {
        var organizerId = GetOrganizerId();
        if (!organizerId.HasValue)
        {
            return Forbid();
        }

        var organizer = await organizerService.GetByIdAsync(organizerId.Value, cancellationToken);
        return organizer is null ? NotFound() : Ok(ToDto(organizer));
    }

    [HttpPut("me")]
    [Authorize(Policy = "Organizer")]
    [EnableRateLimiting("Organizer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateOrganizerDto request,
        CancellationToken cancellationToken)
    {
        var organizerId = GetOrganizerId();
        if (!organizerId.HasValue)
        {
            return Forbid();
        }

        try
        {
            var organizer = await organizerService.UpdateProfileAsync(
                organizerId.Value,
                new Organizer
                {
                    Name = request.Name,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Website = request.Website
                },
                cancellationToken);

            return organizer is null ? NotFound() : NoContent();
        }
        catch (InvalidOperationException)
        {
            return Conflict();
        }
    }

    [HttpDelete("me")]
    [Authorize(Policy = "Organizer")]
    [EnableRateLimiting("Organizer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(OrganizerDeletionConfirmationDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMyAccount(
        [FromQuery] int? confirmEventCount,
        CancellationToken cancellationToken)
    {
        var organizerId = GetOrganizerId();
        if (!organizerId.HasValue)
        {
            return Forbid();
        }

        if (confirmEventCount < 0)
        {
            return BadRequest();
        }

        var eventCount = await organizerService.GetEventCountAsync(organizerId.Value, cancellationToken);
        if (!eventCount.HasValue)
        {
            return NotFound();
        }

        if (!confirmEventCount.HasValue)
        {
            return Conflict(CreateDeletionConfirmation(eventCount.Value));
        }

        var deleted = await organizerService.DeleteOwnAccountAsync(
            organizerId.Value,
            confirmEventCount.Value,
            cancellationToken);
        if (deleted)
        {
            return NoContent();
        }

        var currentEventCount = await organizerService.GetEventCountAsync(organizerId.Value, cancellationToken);
        return currentEventCount.HasValue
            ? Conflict(CreateDeletionConfirmation(currentEventCount.Value))
            : NotFound();
    }

    private Guid? GetOrganizerId()
    {
        return Guid.TryParse(User.FindFirstValue("organizer_id"), out var organizerId)
            ? organizerId
            : null;
    }

    private static OrganizerDeletionConfirmationDto CreateDeletionConfirmation(int eventCount)
    {
        var eventLabel = eventCount == 1 ? "event" : "events";
        return new OrganizerDeletionConfirmationDto
        {
            EventCount = eventCount,
            ConfirmationRequired = true,
            Message = $"Are you sure? This permanently deletes your organizer account and its {eventCount} {eventLabel}. " +
                      $"Retry with ?confirmEventCount={eventCount} to confirm."
        };
    }

    private static OrganizerDto ToDto(Organizer organizer)
    {
        return new OrganizerDto
        {
            Id = organizer.Id,
            Name = organizer.Name,
            Type = organizer.Type,
            Email = organizer.Email,
            PhoneNumber = organizer.PhoneNumber,
            Website = organizer.Website,
            IsActive = organizer.IsActive
        };
    }
}
