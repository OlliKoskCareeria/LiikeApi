using System.ComponentModel.DataAnnotations;
using Liike.Domain;

namespace Liike.Application.Dtos;

public sealed class CreateSportEventDto
{
    [Required]
    [StringLength(200)]
    public required string Title { get; init; }

    [Required]
    public required string Description { get; init; }

    [Required]
    [StringLength(100)]
    public required string Sport { get; init; }

    public required SportCategory Category { get; init; }

    public List<EventTag> Tags { get; init; } = [];

    public required DateTimeOffset StartTime { get; init; }

    public required DateTimeOffset EndTime { get; init; }

    public bool IsRecurring { get; init; }

    [Required]
    [StringLength(120)]
    public required string Municipality { get; init; }

    [Required]
    [StringLength(300)]
    public required string Address { get; init; }

    [Range(typeof(decimal), "0", "9999999999.99")]
    public decimal? Price { get; init; }

    public string? PriceDescription { get; init; }

    public string? RegistrationInfo { get; init; }

    public string? EquipmentInfo { get; init; }

    public string? ArrivalInfo { get; init; }

    public string? AdditionalInfo { get; init; }
}
