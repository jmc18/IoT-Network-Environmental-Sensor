using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using IoTNetwork.Pwa;
using IoTNetwork.Pwa.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBase = builder.Configuration["Api:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBase))
{
    apiBase = builder.HostEnvironment.BaseAddress;
}

builder.Services.AddScoped<IIoTTelemetryApi>(_ => new IoTTelemetryApi(new HttpClient { BaseAddress = new Uri(apiBase, UriKind.Absolute) }));

await builder.Build().RunAsync();
