using FirebaseAdmin.Messaging;
using IoTNetwork.Core.Abstractions.Notifications;
using IoTNetwork.Core.Abstractions.Persistence;
using IoTNetwork.Core.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace IoTNetwork.Infrastructure.Notifications;

/// <summary>
/// Evalúa bandas "High" para Temperatura/CO₂/Ruido y envía push vía FCM a los
/// tokens registrados, con rate-limit de 10 minutos por (nodo, métrica).
/// </summary>
public sealed class FirebasePushNotificationService(
    FirebaseAppInitializer firebase,
    IUnitOfWork uow,
    IMemoryCache cache,
    ILogger<FirebasePushNotificationService> logger) : ICriticalTelemetryNotifier
{
    private static readonly TimeSpan DuplicateSuppression = TimeSpan.FromMinutes(10);

    public async Task NotifyIfCriticalAsync(TelemetryReading reading, CancellationToken cancellationToken = default)
    {
        if (!firebase.IsEnabled || firebase.App is null)
        {
            return;
        }

        var critical = BuildCriticalList(reading);
        if (critical.Count == 0)
        {
            return;
        }

        try
        {
            var tokens = await uow.DeviceTokens.GetForNodeAsync(reading.NodeId, cancellationToken).ConfigureAwait(false);
            if (tokens.Count == 0)
            {
                return;
            }

            foreach (var (metric, display, value, unit) in critical)
            {
                var cacheKey = $"fcm:{reading.NodeId}:{metric}";
                if (cache.TryGetValue(cacheKey, out _))
                {
                    continue;
                }

                cache.Set(cacheKey, true, DuplicateSuppression);

                var message = new MulticastMessage
                {
                    Tokens = tokens.Select(t => t.Token).ToList(),
                    Notification = new Notification
                    {
                        Title = $"IoT crítico: {reading.NodeId}",
                        Body = $"{display} extrema: {value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)}{unit}",
                    },
                    Data = new Dictionary<string, string>
                    {
                        ["nodeId"] = reading.NodeId,
                        ["metric"] = metric,
                        ["value"] = value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                        ["timestampUtc"] = reading.TimestampUtc.ToString("O"),
                    },
                };

                var response = await FirebaseMessaging.GetMessaging(firebase.App)
                    .SendEachForMulticastAsync(message, cancellationToken)
                    .ConfigureAwait(false);

                if (response.FailureCount > 0)
                {
                    logger.LogWarning("FCM falló para {Failed}/{Total} tokens en nodo {NodeId}, métrica {Metric}.",
                        response.FailureCount, tokens.Count, reading.NodeId, metric);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error enviando push FCM para nodo {NodeId}.", reading.NodeId);
        }
    }

    private static List<(string Metric, string Display, double Value, string Unit)> BuildCriticalList(TelemetryReading r)
    {
        var list = new List<(string, string, double, string)>(3);

        if (r.Temperature is { } t && t > 26)
        {
            list.Add(("temperature", "Temperatura", t, " °C"));
        }
        if (r.Co2 is { } c && c > 1200)
        {
            list.Add(("co2", "CO₂", c, " ppm"));
        }
        if (r.NoiseLevel is { } n && n > 60)
        {
            list.Add(("noise", "Ruido", n, " dB"));
        }

        return list;
    }
}
