using IoTNetwork.Pwa.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace IoTNetwork.Pwa.Services;

/// <summary>
/// Cliente SignalR singleton hacia el hub de telemetría del backend.
/// Expone eventos por nodo y global, y actualiza <see cref="AlertState"/>
/// cuando llegan lecturas críticas (con debounce para volver a normal).
/// </summary>
public sealed class TelemetryHubClient : IAsyncDisposable
{
    private static readonly TimeSpan CriticalCooldown = TimeSpan.FromSeconds(30);

    private readonly string _hubUrl;
    private readonly AlertState _alert;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private HubConnection? _connection;
    private DateTime _lastCriticalUtc = DateTime.MinValue;
    private CancellationTokenSource? _cooldownCts;

    public TelemetryHubClient(string apiBaseUrl, AlertState alert, string? apiKey)
    {
        _alert = alert;
        var baseUri = new Uri(apiBaseUrl, UriKind.Absolute);
        var hubUri = new Uri(baseUri, "hubs/telemetry");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            var sep = string.IsNullOrWhiteSpace(hubUri.Query) ? "?" : "&";
            _hubUrl = $"{hubUri}{sep}access_token={Uri.EscapeDataString(apiKey)}";
        }
        else
        {
            _hubUrl = hubUri.ToString();
        }
    }

    public event Action<TelemetryReadingDto>? ReadingReceived;

    public event Action<TelemetryReadingDto>? AnyReadingReceived;

    public HubConnectionState State => _connection?.State ?? HubConnectionState.Disconnected;

    public async Task EnsureStartedAsync(CancellationToken ct = default)
    {
        if (_connection is { State: HubConnectionState.Connected }) return;

        await _startLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_connection is null)
            {
                _connection = new HubConnectionBuilder()
                    .WithUrl(_hubUrl)
                    .WithAutomaticReconnect()
                    .Build();

                _connection.On<TelemetryReadingDto>("reading", OnReading);
                _connection.On<TelemetryReadingDto>("readingAny", OnAnyReading);
            }

            if (_connection.State == HubConnectionState.Disconnected)
            {
                await _connection.StartAsync(ct).ConfigureAwait(false);
            }
        }
        catch
        {
            // Swallow startup errors; UI sigue funcionando vía HTTP.
        }
        finally
        {
            _startLock.Release();
        }
    }

    public async Task JoinNodeAsync(string nodeId, CancellationToken ct = default)
    {
        await EnsureStartedAsync(ct).ConfigureAwait(false);
        if (_connection is { State: HubConnectionState.Connected })
        {
            try
            {
                await _connection.InvokeAsync("JoinNode", nodeId, ct).ConfigureAwait(false);
            }
            catch
            {
            }
        }
    }

    public async Task LeaveNodeAsync(string nodeId, CancellationToken ct = default)
    {
        if (_connection is { State: HubConnectionState.Connected })
        {
            try
            {
                await _connection.InvokeAsync("LeaveNode", nodeId, ct).ConfigureAwait(false);
            }
            catch
            {
            }
        }
    }

    private void OnReading(TelemetryReadingDto dto)
    {
        ReadingReceived?.Invoke(dto);
        EvaluateCritical(dto);
    }

    private void OnAnyReading(TelemetryReadingDto dto)
    {
        AnyReadingReceived?.Invoke(dto);
        EvaluateCritical(dto);
    }

    private void EvaluateCritical(TelemetryReadingDto dto)
    {
        var critical = CriticalState.CriticalMetrics(dto);
        if (critical.Count > 0)
        {
            _lastCriticalUtc = DateTime.UtcNow;
            _alert.SetCritical(true, dto.NodeId, string.Join(", ", critical));
            ScheduleCooldown();
        }
    }

    private void ScheduleCooldown()
    {
        _cooldownCts?.Cancel();
        _cooldownCts = new CancellationTokenSource();
        var token = _cooldownCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(CriticalCooldown, token).ConfigureAwait(false);
                if (DateTime.UtcNow - _lastCriticalUtc >= CriticalCooldown)
                {
                    _alert.SetCritical(false);
                }
            }
            catch (TaskCanceledException)
            {
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        _cooldownCts?.Cancel();
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
        _startLock.Dispose();
    }
}
