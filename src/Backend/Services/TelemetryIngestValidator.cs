using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Lightweight validation for ESP32 payloads.
/// </summary>
public static class TelemetryIngestValidator
{
    public static bool TryValidate(TelemetryIngestRequest request, out string? error)
    {
        error = null;
        if (request.Temperature is < -100 or > 100)
        {
            error = "temperature is out of expected range.";
            return false;
        }

        if (request.Humidity is < 0 or > 100)
        {
            error = "humidity must be between 0 and 100.";
            return false;
        }

        if (request.Co2 is < 0 or > 100_000)
        {
            error = "co2 is out of expected range.";
            return false;
        }

        if (request is { Latitude: not null, Longitude: null } or { Latitude: null, Longitude: not null })
        {
            error = "latitude and longitude must both be provided when using GPS fields.";
            return false;
        }

        if (request is { Latitude: < -90 or > 90 } or { Longitude: < -180 or > 180 })
        {
            error = "coordinates are out of valid ranges.";
            return false;
        }

        return true;
    }
}
