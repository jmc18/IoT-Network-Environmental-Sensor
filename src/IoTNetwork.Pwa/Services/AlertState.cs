namespace IoTNetwork.Pwa.Services;

/// <summary>
/// Estado global de alerta: se activa cuando se recibe en vivo una lectura
/// con al menos una métrica crítica (banda High de Temp/CO2/Ruido).
/// El layout lo usa para aplicar el "flash rojo global".
/// </summary>
public sealed class AlertState
{
    private readonly object _lock = new();
    private bool _critical;
    private string? _currentNodeId;
    private string? _currentMetrics;

    public bool Critical => _critical;

    public string? CurrentNodeId => _currentNodeId;

    public string? CurrentMetrics => _currentMetrics;

    public event Action? Changed;

    public void SetCritical(bool value, string? nodeId = null, string? metrics = null)
    {
        bool changed;
        lock (_lock)
        {
            changed = _critical != value
                || !string.Equals(_currentNodeId, nodeId, StringComparison.Ordinal)
                || !string.Equals(_currentMetrics, metrics, StringComparison.Ordinal);
            _critical = value;
            _currentNodeId = value ? nodeId : null;
            _currentMetrics = value ? metrics : null;
        }

        if (changed)
        {
            Changed?.Invoke();
        }
    }
}
