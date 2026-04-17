using System.Net.Http.Json;
using IoTNetwork.Pwa.Models;

namespace IoTNetwork.Pwa.Services;

public sealed class IoTTelemetryApi(HttpClient http) : IIoTTelemetryApi
{
    public async Task<IReadOnlyList<string>> GetNodesAsync(CancellationToken cancellationToken = default)
    {
        var res = await http.GetFromJsonAsync<NodesListDto>("/api/nodes", cancellationToken).ConfigureAwait(false);
        return res?.Nodes ?? [];
    }

    public async Task<IReadOnlyList<TelemetryReadingDto>> GetReadingsAsync(
        string nodeId,
        DateTime fromUtc,
        DateTime toUtc,
        int maxItems,
        CancellationToken cancellationToken = default)
    {
        var from = Uri.EscapeDataString(fromUtc.ToUniversalTime().ToString("O"));
        var to = Uri.EscapeDataString(toUtc.ToUniversalTime().ToString("O"));
        var url = $"/api/nodes/{Uri.EscapeDataString(nodeId)}/readings?from={from}&to={to}&maxItems={maxItems}";
        var res = await http.GetFromJsonAsync<PagedReadingsDto>(url, cancellationToken).ConfigureAwait(false);
        return res?.Items ?? [];
    }

    public async Task<IReadOnlyList<string>> GetAvailableDatesAsync(string nodeId, CancellationToken cancellationToken = default)
    {
        var url = $"/api/nodes/{Uri.EscapeDataString(nodeId)}/available-dates";
        var res = await http.GetFromJsonAsync<AvailableDatesDto>(url, cancellationToken).ConfigureAwait(false);
        return res?.Dates ?? [];
    }
}
