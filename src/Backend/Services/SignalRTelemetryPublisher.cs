using Backend.Infrastructure;
using Backend.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;

namespace Backend.Services;

/// <summary>
/// Sends hub messages through Azure SignalR Management SDK (serverless-compatible).
/// </summary>
public sealed class SignalRTelemetryPublisher : ISignalRTelemetryPublisher, IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly ILogger<SignalRTelemetryPublisher> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IServiceHubContext? _hubContext;

    public SignalRTelemetryPublisher(string connectionString, ILogger<SignalRTelemetryPublisher> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task PublishReadingAsync(TelemetryUpdatedPayload payload, CancellationToken cancellationToken)
    {
        var hub = await GetHubContextAsync(cancellationToken).ConfigureAwait(false);
        await hub.Clients
            .User(payload.NodeId)
            .SendAsync(SignalRConstants.TelemetryUpdatedTarget, payload, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IServiceHubContext> GetHubContextAsync(CancellationToken cancellationToken)
    {
        if (_hubContext is not null)
        {
            return _hubContext;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_hubContext is not null)
            {
                return _hubContext;
            }

            var serviceManager = new ServiceManagerBuilder()
                .WithOptions(o => o.ConnectionString = _connectionString)
                .BuildServiceManager();

            _hubContext = await serviceManager
                .CreateHubContextAsync(SignalRConstants.HubName, cancellationToken)
                .ConfigureAwait(false);

            return _hubContext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Azure SignalR hub context.");
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubContext is not null)
        {
            await _hubContext.DisposeAsync().ConfigureAwait(false);
        }

        _gate.Dispose();
    }
}
