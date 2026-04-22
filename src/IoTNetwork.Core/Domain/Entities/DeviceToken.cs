namespace IoTNetwork.Core.Domain.Entities;

/// <summary>
/// Token FCM registrado por un navegador / PWA para recibir notificaciones push
/// cuando se detectan lecturas críticas.
/// </summary>
public sealed class DeviceToken
{
    public Guid Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public string? NodeFilter { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime LastSeenUtc { get; set; }
}
