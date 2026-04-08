using Backend.Configuration;
using Backend.Data;
using Backend.Infrastructure;
using Backend.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.Configure<CosmosOptions>(builder.Configuration.GetSection(CosmosOptions.SectionName));

builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CosmosOptions>>().Value;
    if (string.IsNullOrWhiteSpace(options.ConnectionString))
    {
        throw new InvalidOperationException("Cosmos:ConnectionString is required (see local.settings.json or App Settings).");
    }

    return new CosmosClient(options.ConnectionString);
});

builder.Services.AddSingleton<ICosmosTelemetryStore, CosmosTelemetryStore>();

var signalRConnection = builder.Configuration[SignalRConstants.ConnectionSetting];
if (string.IsNullOrWhiteSpace(signalRConnection))
{
    builder.Services.AddSingleton<ISignalRTelemetryPublisher, NoOpSignalRTelemetryPublisher>();
}
else
{
    builder.Services.AddSingleton<ISignalRTelemetryPublisher>(sp =>
        new SignalRTelemetryPublisher(
            signalRConnection,
            sp.GetRequiredService<ILogger<SignalRTelemetryPublisher>>()));
}

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
