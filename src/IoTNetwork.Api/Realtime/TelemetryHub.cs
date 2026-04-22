using Microsoft.AspNetCore.SignalR;

namespace IoTNetwork.Api.Realtime;

/// <summary>
/// Hub de telemetría en tiempo real.
/// Los clientes pueden unirse/salir de un grupo por nodeId para recibir solo lecturas
/// de un nodo concreto ("reading"). También se emite "readingAny" a todos.
/// </summary>
public sealed class TelemetryHub : Hub
{
    public Task JoinNode(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return Task.CompletedTask;
        }

        return Groups.AddToGroupAsync(Context.ConnectionId, GroupName(nodeId));
    }

    public Task LeaveNode(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return Task.CompletedTask;
        }

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(nodeId));
    }

    public static string GroupName(string nodeId) => $"node:{nodeId.Trim()}";
}
