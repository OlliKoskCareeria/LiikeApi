namespace Liike.Application.Dtos;

public sealed class NearbySportEventDto
{
    public PublicSportEventDto Event { get; init; } = null!;

    public double DistanceKilometers { get; init; }
}
