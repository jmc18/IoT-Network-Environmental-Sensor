namespace IoTNetwork.Api.Middleware;

public sealed class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
{
    private const string HeaderName = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        var expected = configuration["Ingest:ApiKey"];
        if (string.IsNullOrWhiteSpace(expected))
        {
            logger.LogWarning("Ingest:ApiKey is not configured; rejecting ingest requests.");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Ingest API key is not configured on the server.").ConfigureAwait(false);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var provided) || provided != expected)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid or missing API key.").ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }
}
