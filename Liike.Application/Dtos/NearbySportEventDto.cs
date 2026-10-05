namespace Liike.Application.Dtos;

public sealed class NearbySportEventDto
{
    public SportEventDto Event { get; init; } = null!;

    public double DistanceKilometers { get; init; }
}
