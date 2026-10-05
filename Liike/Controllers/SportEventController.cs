using Liike.Application.Dtos;
using Liike.Application.Services;
using Liike.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Liike.Controllers;

/// <summary>Provides published-event browsing and organizer event operations.</summary>
[ApiController]
[Route("api/sport-events")]
[Produces("application/json")]
public sealed class SportEventController(
    IPublicEventService publicEventService,
    IOrganizerEventService organizerEventService) : ControllerBase
{
    /// <summary>Gets all published events without applying search filters.</summary>
    /// <remarks>
    /// This endpoint returns published events without any filters. It is
    /// intended for use cases where a complete list of events is needed,
    /// e.g., for administrative purposes.
    /// </remarks>
    [HttpGet("all")]
    [ProducesResponseType(typeof(IReadOnlyList<SportEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SportEventDto>>> GetAllPublishedWithoutFilters(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var events = await publicEventService.SearchPublishedAsync(
                pageNumber: pageNumber,
                pageSize: pageSize,
                cancellationToken: cancellationToken);

            return Ok(events.Select(ToDto).ToList());
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    /// <summary>Gets published events with optional filters and pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SportEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SportEventDto>>> GetAllPublished(
        [FromQuery] string? municipality,
        [FromQuery] SportCategory? category,
        [FromQuery] decimal? minimumPrice,
        [FromQuery] decimal? maximumPrice,
        [FromQuery] OrganizerType? organizerType,
        [FromQuery] EventTag? tag,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var events = await publicEventService.SearchPublishedAsync(
                municipality,
                category,
                minimumPrice,
                maximumPrice,
                organizerType,
                tag,
                pageNumber,
                pageSize,
                cancellationToken);

            return Ok(events.Select(ToDto).ToList());
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    /// <summary>Gets a published event by its identifier.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SportEventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SportEventDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        var sportEvent = await publicEventService.GetPublishedByIdAsync(id, cancellationToken);
        return sportEvent is null ? NotFound() : Ok(ToDto(sportEvent));
    }

    /// <summary>
    /// Creates an event for the supplied organizer. OrganizerId is a query parameter until authentication is implemented.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SportEventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<SportEventDto>> Create(
        [FromQuery] Guid organizerId,
        [FromBody] CreateSportEventDto request,
        CancellationToken cancellationToken)
    {
        if (organizerId == Guid.Empty)
        {
            return BadRequest();
        }

        try
        {
            var sportEvent = await organizerEventService.CreateAsync(
                organizerId,
                ToEntity(request),
                cancellationToken);
            var response = ToDto(sportEvent);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (GeocodingProviderUnavailableException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        catch (AddressCouldNotBeGeocodedException)
        {
            return UnprocessableEntity();
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

    /// <summary>
    /// Replaces an event's editable fields. OrganizerId is a query parameter until authentication is implemented.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromQuery] Guid organizerId,
        [FromBody] UpdateSportEventDto request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || organizerId == Guid.Empty)
        {
            return BadRequest();
        }

        try
        {
            var sportEvent = await organizerEventService.UpdateAsync(
                organizerId,
                id,
                ToEntity(request),
                cancellationToken);
            return sportEvent is null ? NotFound() : NoContent();
        }
        catch (GeocodingProviderUnavailableException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        catch (AddressCouldNotBeGeocodedException)
        {
            return UnprocessableEntity();
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

    /// <summary>Deletes an event owned by the supplied organizer.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromQuery] Guid organizerId,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || organizerId == Guid.Empty)
        {
            return BadRequest();
        }

        var deleted = await organizerEventService.DeleteAsync(organizerId, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private static SportEvent ToEntity(CreateSportEventDto request)
    {
        return new SportEvent
        {
            Title = request.Title,
            Description = request.Description,
            Sport = request.Sport,
            Category = request.Category,
            Tags = request.Tags.Select(tag => new SportEventTag { Tag = tag }).ToList(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsRecurring = request.IsRecurring,
            Municipality = request.Municipality,
            Address = request.Address,
            Price = request.Price,
            PriceDescription = request.PriceDescription,
            RegistrationInfo = request.RegistrationInfo,
            EquipmentInfo = request.EquipmentInfo,
            ArrivalInfo = request.ArrivalInfo,
            AdditionalInfo = request.AdditionalInfo
        };
    }

    private static SportEvent ToEntity(UpdateSportEventDto request)
    {
        return new SportEvent
        {
            Title = request.Title,
            Description = request.Description,
            Sport = request.Sport,
            Category = request.Category,
            Tags = request.Tags.Select(tag => new SportEventTag { Tag = tag }).ToList(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsRecurring = request.IsRecurring,
            Municipality = request.Municipality,
            Address = request.Address,
            Price = request.Price,
            PriceDescription = request.PriceDescription,
            RegistrationInfo = request.RegistrationInfo,
            EquipmentInfo = request.EquipmentInfo,
            ArrivalInfo = request.ArrivalInfo,
            AdditionalInfo = request.AdditionalInfo
        };
    }

    private static SportEventDto ToDto(SportEvent sportEvent)
    {
        
        return new SportEventDto
        {
            Id = sportEvent.Id,
            OrganizerId = sportEvent.OrganizerId,
            OrganizerName = sportEvent.Organizer?.Name ?? string.Empty,
            OrganizerType = sportEvent.Organizer?.Type ?? OrganizerType.Unspecified,
            Title = sportEvent.Title,
            Description = sportEvent.Description,
            Sport = sportEvent.Sport,
            Category = sportEvent.Category,
            Tags = sportEvent.Tags.Select(tag => tag.Tag).ToArray(),
            StartTime = sportEvent.StartTime,
            EndTime = sportEvent.EndTime,
            IsRecurring = sportEvent.IsRecurring,
            IsPublished = sportEvent.IsPublished,
            Municipality = sportEvent.Municipality,
            Address = sportEvent.Address,
            Latitude = sportEvent.Latitude,
            Longitude = sportEvent.Longitude,
            Price = sportEvent.Price,
            PriceDescription = sportEvent.PriceDescription,
            RegistrationInfo = sportEvent.RegistrationInfo,
            EquipmentInfo = sportEvent.EquipmentInfo,
            ArrivalInfo = sportEvent.ArrivalInfo,
            AdditionalInfo = sportEvent.AdditionalInfo
        };
    }
}
