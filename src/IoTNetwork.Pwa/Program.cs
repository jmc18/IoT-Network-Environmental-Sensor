using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using IoTNetwork.Pwa;
using IoTNetwork.Pwa.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ApiSettings (estilo A_PG) con compatibilidad Api:BaseUrl
var apiBase = builder.Configuration["ApiSettings:BaseUrl"]
    ?? builder.Configuration["Api:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBase))
{
    apiBase = builder.HostEnvironment.BaseAddress;
}

var timeoutSeconds = builder.Configuration.GetValue("ApiSettings:Timeout", 30);
var apiKey = builder.Configuration["ApiSettings:ApiKey"] ?? builder.Configuration["Api:ApiKey"];

HttpClient BuildApiClient()
{
    var client = new HttpClient
    {
        BaseAddress = new Uri(apiBase, UriKind.Absolute),
        Timeout = TimeSpan.FromSeconds(timeoutSeconds)
    };
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", apiKey);
    }

    return client;
}

builder.Services.AddSingleton<ThemeService>();
builder.Services.AddSingleton<AlertState>();
builder.Services.AddSingleton(sp => new TelemetryHubClient(
    apiBase,
    sp.GetRequiredService<AlertState>(),
    apiKey));
builder.Services.AddSingleton(sp => new PushRegistrationService(
    sp.GetRequiredService<IJSRuntime>(),
    apiBase,
    apiKey,
    builder.Configuration,
    sp.GetService<ILogger<PushRegistrationService>>()));
builder.Services.AddScoped<IIoTTelemetryApi>(_ => new IoTTelemetryApi(BuildApiClient()));

await builder.Build().RunAsync();
