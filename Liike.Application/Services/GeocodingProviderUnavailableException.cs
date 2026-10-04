namespace Liike.Application.Services;

public sealed class GeocodingProviderUnavailableException : InvalidOperationException
{
    public GeocodingProviderUnavailableException()
        : base("No geocoding provider is configured.")
    {
    }
}
