using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace IoTBackend.Shared.Security;

/// <summary>
/// ASP.NET Core middleware that validates X-Api-Key header for ingest endpoints
/// </summary>
public class ApiKeyAuthenticationMiddleware
{
    public const string ApiKeyHeaderName = "X-Api-Key";

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration config)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.Contains("/api/ingest", StringComparison.OrdinalIgnoreCase))
        {
            var expectedKey = config["IOT_INGEST_API_KEY"] ?? config["IotIngest:ApiKey"];

            if (string.IsNullOrEmpty(expectedKey))
            {
                _logger.LogWarning("IOT_INGEST_API_KEY is not configured. Ingest endpoint will reject all requests.");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = "Server configuration error" });
                return;
            }

            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var headerValue) || headerValue != expectedKey)
            {
                _logger.LogWarning("Ingest request rejected: invalid or missing API key");
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing API key" });
                return;
            }
        }

        await _next(context);
    }
}
