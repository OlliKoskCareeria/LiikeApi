using System.Globalization;
using System.Text.Json;
using System.Net.Http.Headers;
using Liike.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Liike.Infrastructure.Services;

public sealed class NominatimGeocodingService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<NominatimGeocodingService> logger) : IGeocodingService
{
    private const string DefaultBaseUrl = "https://nominatim.openstreetmap.org/";
    private static readonly SemaphoreSlim RequestGate = new(1, 1);
    private static DateTimeOffset _lastRequestStartedAt = DateTimeOffset.MinValue;

    private readonly string _baseUrl = configuration["Geocoding:Nominatim:BaseUrl"] ?? DefaultBaseUrl;
    private readonly string? _userAgent = configuration["Geocoding:Nominatim:UserAgent"];

    public async Task<(double Latitude, double Longitude)?> GeocodeAsync(
        string address,
        string municipality,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_userAgent))
        {
            logger.LogError("Nominatim User-Agent is not configured.");
            throw new GeocodingProviderUnavailableException();
        }

        if (!Uri.TryCreate(_baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps)
        {
            logger.LogError("The configured Nominatim base URL is invalid or does not use HTTPS.");
            throw new GeocodingProviderUnavailableException();
        }

        var query = Uri.EscapeDataString($"{address}, {municipality}");
        var requestUri = new Uri(baseUri, $"search?format=jsonv2&limit=1&q={query}");

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        try
        {
            request.Headers.UserAgent.ParseAdd(_userAgent);
        }
        catch (FormatException exception)
        {
            logger.LogError(exception, "The configured Nominatim User-Agent is invalid.");
            throw new GeocodingProviderUnavailableException();
        }

        try
        {
            await WaitForRateLimitAsync(cancellationToken);
            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Nominatim returned HTTP status {StatusCode}.", (int)response.StatusCode);
                throw new GeocodingProviderUnavailableException();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (document.RootElement.ValueKind != JsonValueKind.Array ||
                document.RootElement.GetArrayLength() == 0)
            {
                return null;
            }

            var result = document.RootElement[0];
            if (!result.TryGetProperty("lat", out var latitudeElement) ||
                !result.TryGetProperty("lon", out var longitudeElement) ||
                !double.TryParse(latitudeElement.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
                !double.TryParse(longitudeElement.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude) ||
                !double.IsFinite(latitude) ||
                !double.IsFinite(longitude) ||
                latitude is < -90 or > 90 ||
                longitude is < -180 or > 180)
            {
                return null;
            }

            return (latitude, longitude);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "The Nominatim request failed.");
            throw new GeocodingProviderUnavailableException();
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Reading the Nominatim response failed.");
            throw new GeocodingProviderUnavailableException();
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "The Nominatim request timed out.");
            throw new GeocodingProviderUnavailableException();
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Nominatim returned an invalid response.");
            throw new GeocodingProviderUnavailableException();
        }
    }

    private static async Task WaitForRateLimitAsync(CancellationToken cancellationToken)
    {
        await RequestGate.WaitAsync(cancellationToken);
        try
        {
            var delay = TimeSpan.FromSeconds(1) - (DateTimeOffset.UtcNow - _lastRequestStartedAt);
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken);
            }

            _lastRequestStartedAt = DateTimeOffset.UtcNow;
        }
        finally
        {
            RequestGate.Release();
        }
    }
}
