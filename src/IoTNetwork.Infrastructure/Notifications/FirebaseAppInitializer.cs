using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IoTNetwork.Infrastructure.Notifications;

/// <summary>
/// Inicializa una única instancia de <see cref="FirebaseApp"/>.
/// Intenta, en orden:
///   1. Ruta indicada en <c>Firebase:ServiceAccountPath</c> (absoluta, relativa al cwd, o relativa al AppContext.BaseDirectory).
///   2. Variable de entorno <c>GOOGLE_APPLICATION_CREDENTIALS</c> (comportamiento por defecto del SDK).
/// </summary>
public sealed class FirebaseAppInitializer
{
    public FirebaseAppInitializer(IConfiguration configuration, ILogger<FirebaseAppInitializer> logger)
    {
        var enabled = configuration.GetValue("Firebase:Enabled", false);
        if (!enabled)
        {
            IsEnabled = false;
            return;
        }

        var configured = configuration["Firebase:ServiceAccountPath"];
        var resolved = ResolvePath(configured);

        try
        {
            var credential = resolved is not null
                ? GoogleCredential.FromFile(resolved)
                : GoogleCredential.GetApplicationDefault();

            App = FirebaseApp.DefaultInstance ?? FirebaseApp.Create(new AppOptions
            {
                Credential = credential,
            });
            IsEnabled = App is not null;

            if (IsEnabled)
            {
                logger.LogInformation("Firebase Admin inicializado (credencial: {Source}).",
                    resolved ?? "GOOGLE_APPLICATION_CREDENTIALS");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo inicializar Firebase Admin SDK (ruta evaluada: {Path}).", resolved ?? configured);
            IsEnabled = false;
        }
    }

    public FirebaseApp? App { get; }

    public bool IsEnabled { get; }

    private static string? ResolvePath(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured)) return null;

        if (Path.IsPathRooted(configured) && File.Exists(configured))
        {
            return configured;
        }

        var fromCwd = Path.GetFullPath(configured);
        if (File.Exists(fromCwd)) return fromCwd;

        var fromBase = Path.Combine(AppContext.BaseDirectory, configured);
        if (File.Exists(fromBase)) return fromBase;

        return null;
    }
}
