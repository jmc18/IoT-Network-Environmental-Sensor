using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Used when Azure SignalR is not configured (local dev without the service).
/// </summary>
public sealed class NoOpSignalRTelemetryPublisher : ISignalRTelemetryPublisher
{
    public Task PublishReadingAsync(TelemetryUpdatedPayload payload, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
