using Liike.Application.Dtos;
using Liike.Application.Services;
using Liike.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Liike.Controllers;

/// <summary>
/// Provides organizer management endpoints. These endpoints are unauthenticated until authentication is added.
/// </summary>
[ApiController]
[Route("api/organizers")]
[Produces("application/json")]
public sealed class OrganizerController(IOrganizerService organizerService) : ControllerBase
{
    /// <summary>Gets all organizers, including inactive organizers.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrganizerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrganizerDto>>> GetAll(CancellationToken cancellationToken)
    {
        var organizers = await organizerService.GetAllAsync(cancellationToken);
        return Ok(organizers.Select(ToDto).ToList());
    }

    /// <summary>Gets an organizer by its identifier.</summary>
    [HttpGet("{id:guid}")]
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
    [ProducesResponseType(typeof(OrganizerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrganizerDto>> Create(
        [FromBody] CreateOrganizerDto request,
        CancellationToken cancellationToken)
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
            cancellationToken);

        var response = ToDto(organizer);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>Replaces an organizer's editable profile fields.</summary>
    [HttpPut("{id:guid}")]
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

    /// <summary>Deletes an organizer that has no associated events.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
