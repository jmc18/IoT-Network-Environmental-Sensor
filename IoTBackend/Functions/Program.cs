using Functions.Services;
using IoTBackend.Shared.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        var config = context.Configuration;
        var cosmosConnectionString = config["CosmosDb__ConnectionString"] ?? config["COSMOS_CONNECTION_STRING"];

        if (!string.IsNullOrEmpty(cosmosConnectionString))
        {
            services.AddSingleton(sp =>
            {
                var options = new CosmosClientOptions { ApplicationName = "IoT-Network-Backend" };
                return new CosmosClient(cosmosConnectionString, options);
            });
            services.AddSingleton<ICosmosDbService, CosmosDbService>();
            services.AddHostedService<CosmosDbInitializer>();
        }
        else
        {
            services.AddSingleton<ICosmosDbService, NullCosmosDbService>();
        }

        var signalRConnectionString = config["AzureSignalRConnectionString"]
            ?? config["SignalR__ConnectionString"]
            ?? config["SIGNALR_CONNECTION_STRING"];
        if (!string.IsNullOrEmpty(signalRConnectionString))
        {
            services.AddSingleton<ISignalRService, SignalRService>();
        }
        else
        {
            services.AddSingleton<ISignalRService, NullSignalRService>();
        }
    })
    .Build();

host.Run();
