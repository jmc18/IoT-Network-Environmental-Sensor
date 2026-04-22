using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace IoTNetwork.Pwa.Services;

/// <summary>
/// Coordina la obtención del token FCM vía JS y lo registra en el backend.
/// Lee configuración de Firebase Web desde <c>FirebaseWeb:*</c>.
/// Guarda el último token en localStorage para evitar re-registrar el mismo
/// (mismo patrón que CICA.PWA).
/// </summary>
public sealed class PushRegistrationService
{
    private const string LocalStorageKey = "iot.fcm.token";

    private readonly IJSRuntime _js;
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<PushRegistrationService>? _logger;
    private bool _triedRegister;

    public PushRegistrationService(
        IJSRuntime js,
        string apiBaseUrl,
        IConfiguration config,
        ILogger<PushRegistrationService>? logger = null)
    {
        _js = js;
        _config = config;
        _logger = logger;
        _http = new HttpClient { BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute) };
    }

    public string? CurrentToken { get; private set; }

    public async Task TryRegisterAsync(string? nodeFilter = null)
    {
        if (_triedRegister) return;
        _triedRegister = true;

        var cfg = ReadConfig();
        if (cfg is null)
        {
            _logger?.LogInformation("FirebaseWeb:ApiKey vacío; registro FCM omitido.");
            return;
        }

        try
        {
            var (firebaseConfig, vapidKey) = cfg.Value;
            if (string.IsNullOrWhiteSpace(vapidKey))
            {
                _logger?.LogWarning("FirebaseWeb:VapidKey vacío; el token FCM no se podrá obtener.");
            }

            var token = await _js.InvokeAsync<string?>("iotFcmInit", firebaseConfig, vapidKey, null);
            if (string.IsNullOrWhiteSpace(token)) return;

            CurrentToken = token;

            var cached = await TryGetLocalStorageAsync(LocalStorageKey);
            if (string.Equals(cached, token, StringComparison.Ordinal))
            {
                return;
            }

            var resp = await _http.PostAsJsonAsync("/api/push/register", new
            {
                token,
                nodeFilter,
            });
            resp.EnsureSuccessStatusCode();

            await TrySetLocalStorageAsync(LocalStorageKey, token);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error registrando token FCM.");
        }
    }

    private (Dictionary<string, string?> Config, string? VapidKey)? ReadConfig()
    {
        var section = _config.GetSection("FirebaseWeb");
        var apiKey = section["ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        var config = new Dictionary<string, string?>
        {
            ["apiKey"] = apiKey,
            ["authDomain"] = section["AuthDomain"],
            ["projectId"] = section["ProjectId"],
            ["storageBucket"] = section["StorageBucket"],
            ["messagingSenderId"] = section["MessagingSenderId"],
            ["appId"] = section["AppId"],
            ["measurementId"] = section["MeasurementId"],
        };

        return (config, section["VapidKey"]);
    }

    private async Task<string?> TryGetLocalStorageAsync(string key)
    {
        try
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", key);
        }
        catch
        {
            return null;
        }
    }

    private async Task TrySetLocalStorageAsync(string key, string value)
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        catch
        {
        }
    }
}
