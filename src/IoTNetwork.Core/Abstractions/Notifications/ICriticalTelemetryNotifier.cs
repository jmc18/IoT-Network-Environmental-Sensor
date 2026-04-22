using IoTNetwork.Core.Domain.Entities;

namespace IoTNetwork.Core.Abstractions.Notifications;

/// <summary>
/// Abstracción consumida por la capa API para notificar lecturas críticas
/// sin acoplarse a Firebase. La implementación concreta vive en Infrastructure.
/// </summary>
public interface ICriticalTelemetryNotifier
{
    Task NotifyIfCriticalAsync(TelemetryReading reading, CancellationToken cancellationToken = default);
}
