using IoTBackend.Shared.Models;

namespace IoTBackend.Shared.Services;

public interface ISignalRService
{
    Task PublishNewReadingAsync(NodeDocument reading, CancellationToken cancellationToken = default);
}
