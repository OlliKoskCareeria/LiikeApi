namespace Liike.Application.Services;

public interface IGeocodingService
{
    Task<(double Latitude, double Longitude)?> GeocodeAsync(
        string address,
        string municipality,
        CancellationToken cancellationToken = default);
}
