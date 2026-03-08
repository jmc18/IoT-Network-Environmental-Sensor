using System.Text.Json;
using IoTBackend.Shared.Models;
using IoTBackend.Shared.Services;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;

namespace Functions.Services;

public class SignalRService : ISignalRService
{
    private const string HubName = "iot-map";
    private readonly ServiceManager _serviceManager;
    private readonly IConfiguration _configuration;

    public SignalRService(IConfiguration configuration)
    {
        _configuration = configuration;
        var connectionString = configuration["AzureSignalRConnectionString"]
            ?? configuration["SignalR__ConnectionString"]
            ?? configuration["SIGNALR_CONNECTION_STRING"];
        _serviceManager = new ServiceManagerBuilder()
            .WithOptions(option => option.ConnectionString = connectionString)
            .BuildServiceManager();
    }

    public async Task PublishNewReadingAsync(NodeDocument reading, CancellationToken cancellationToken = default)
    {
        await using var hubContext = await _serviceManager.CreateHubContextAsync(HubName, cancellationToken);
        var payload = JsonSerializer.Serialize(reading);
        await hubContext.Clients.All.SendCoreAsync("NewReading", new object[] { payload }, cancellationToken);
    }
}
