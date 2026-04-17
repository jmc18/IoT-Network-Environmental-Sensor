using Backend.Models;

namespace Backend.Services;

public interface ISignalRTelemetryPublisher
{
    Task PublishReadingAsync(TelemetryUpdatedPayload payload, CancellationToken cancellationToken);
}
