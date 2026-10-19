using Liike.Application.Dtos;
using Liike.Application.Services;
using Liike.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
    [AllowAnonymous]
    [EnableRateLimiting("PublicSearch")]
    [ProducesResponseType(typeof(IReadOnlyList<PublicSportEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<PublicSportEventDto>>> GetAllPublishedWithoutFilters(
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

            return Ok(events.Select(ToPublicDto).ToList());
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    /// <summary>Gets published events with optional filters and pagination.</summary>
    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting("PublicSearch")]
    [ProducesResponseType(typeof(IReadOnlyList<PublicSportEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<PublicSportEventDto>>> GetAllPublished(
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

            return Ok(events.Select(ToPublicDto).ToList());
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    /// <summary>Gets published events ordered by distance from the supplied coordinates.</summary>
    [HttpGet("nearby")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicSearch")]
    [ProducesResponseType(typeof(IReadOnlyList<NearbySportEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<NearbySportEventDto>>> GetNearbyPublished(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double? radiusKilometers = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var events = await publicEventService.SearchNearbyPublishedAsync(
                latitude,
                longitude,
                radiusKilometers,
                cancellationToken);

            return Ok(events.Select(result => new NearbySportEventDto
            {
                Event = ToPublicDto(result.Event),
                DistanceKilometers = result.DistanceKilometers
            }).ToList());
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    /// <summary>Gets a published event by its identifier.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicSearch")]
    [ProducesResponseType(typeof(PublicSportEventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicSportEventDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        var sportEvent = await publicEventService.GetPublishedByIdAsync(id, cancellationToken);
        return sportEvent is null ? NotFound() : Ok(ToPublicDto(sportEvent));
    }

    /// <summary>Gets events owned by the authenticated organizer.</summary>
    [HttpGet("mine")]
    [Authorize(Policy = "Organizer")]
    [EnableRateLimiting("Organizer")]
    [ProducesResponseType(typeof(IReadOnlyList<SportEventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SportEventDto>>> GetOwnEvents(
        CancellationToken cancellationToken)
    {
        var organizerId = GetOrganizerId();
        if (!organizerId.HasValue)
        {
            return Forbid();
        }

        var events = await organizerEventService.GetOwnEventsAsync(organizerId.Value, cancellationToken);
        return Ok(events.Select(ToDto).ToList());
    }

    /// <summary>
    /// Creates an event for the authenticated organizer.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "Organizer")]
    [EnableRateLimiting("Organizer")]
    [ProducesResponseType(typeof(SportEventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<SportEventDto>> Create(
        [FromBody] CreateSportEventDto request,
        CancellationToken cancellationToken)
    {
        var organizerId = GetOrganizerId();
        if (!organizerId.HasValue)
        {
            return Forbid();
        }

        try
        {
            var sportEvent = await organizerEventService.CreateAsync(
                organizerId.Value,
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
    /// Replaces an event's editable fields for the authenticated organizer.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Organizer")]
    [EnableRateLimiting("Organizer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateSportEventDto request,
        CancellationToken cancellationToken)
    {
        var organizerId = GetOrganizerId();
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        if (!organizerId.HasValue)
        {
            return Forbid();
        }

        try
        {
            var sportEvent = await organizerEventService.UpdateAsync(
                organizerId.Value,
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
    [Authorize(Policy = "Organizer")]
    [EnableRateLimiting("Organizer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var organizerId = GetOrganizerId();
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        if (!organizerId.HasValue)
        {
            return Forbid();
        }

        var deleted = await organizerEventService.DeleteAsync(organizerId.Value, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private Guid? GetOrganizerId()
    {
        return Guid.TryParse(User.FindFirstValue("organizer_id"), out var organizerId)
            ? organizerId
            : null;
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

    private static PublicSportEventDto ToPublicDto(SportEvent sportEvent)
    {
        return new PublicSportEventDto
        {
            Id = sportEvent.Id,
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
