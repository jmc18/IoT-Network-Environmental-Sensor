using Microsoft.AspNetCore.Builder;

namespace IoTBackend.Shared.Security;

public static class SecurityExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
    }
}
