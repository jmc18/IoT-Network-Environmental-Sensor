namespace IoTNetwork.Api.Middleware;

public sealed class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
{
    private const string HeaderName = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        var expected = configuration["Security:ApiKey"] ?? configuration["Ingest:ApiKey"];
        if (string.IsNullOrWhiteSpace(expected))
        {
            logger.LogWarning("Security:ApiKey/Ingest:ApiKey is not configured; rejecting secured requests.");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("API key is not configured on the server.").ConfigureAwait(false);
            return;
        }

        var provided = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? context.Request.Query["apikey"].FirstOrDefault()
            ?? context.Request.Query["access_token"].FirstOrDefault();

        if (!string.Equals(provided, expected, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid or missing API key.").ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }
}
