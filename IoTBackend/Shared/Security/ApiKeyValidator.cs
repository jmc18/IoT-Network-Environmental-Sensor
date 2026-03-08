using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace IoTBackend.Shared.Security;

public static class ApiKeyValidator
{
    public const string ApiKeyHeaderName = "X-Api-Key";

    /// <summary>
    /// Validates the API key from the request. Returns true if valid, false otherwise.
    /// If false, the caller should return 401 and not proceed.
    /// </summary>
    public static bool Validate(HttpRequest request, IConfiguration configuration)
    {
        var expectedKey = configuration["IOT_INGEST_API_KEY"] ?? configuration["IotIngest:ApiKey"];
        if (string.IsNullOrEmpty(expectedKey))
            return false;

        return request.Headers.TryGetValue(ApiKeyHeaderName, out var headerValue) && headerValue == expectedKey;
    }
}
