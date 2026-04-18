using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
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
builder.Services.AddSingleton<ThemeService>();
builder.Services.AddScoped<IIoTTelemetryApi>(_ => new IoTTelemetryApi(new HttpClient
{
    BaseAddress = new Uri(apiBase, UriKind.Absolute),
    Timeout = TimeSpan.FromSeconds(timeoutSeconds)
}));

await builder.Build().RunAsync();
