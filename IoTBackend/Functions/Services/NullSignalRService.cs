using IoTBackend.Shared.Models;
using IoTBackend.Shared.Services;

namespace Functions.Services;

/// <summary>
/// No-op implementation when SignalR is not configured
/// </summary>
public class NullSignalRService : ISignalRService
{
    public Task PublishNewReadingAsync(NodeDocument reading, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
