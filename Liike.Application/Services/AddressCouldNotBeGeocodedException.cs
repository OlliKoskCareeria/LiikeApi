namespace Liike.Application.Services;

public sealed class AddressCouldNotBeGeocodedException : InvalidOperationException
{
    public AddressCouldNotBeGeocodedException()
        : base("The supplied address could not be resolved to coordinates.")
    {
    }
}
