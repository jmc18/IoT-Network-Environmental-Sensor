namespace IoTBackend.Shared.Models;

public static class NodeReadingExtensions
{
    public static NodeDocument ToNodeDocument(this NodeReadingRequest request)
    {
        var timestamp = request.Timestamp ?? DateTimeOffset.UtcNow;
        var doc = new NodeDocument
        {
            Id = $"{request.NodeId}_{timestamp:O}",
            NodeId = request.NodeId,
            Timestamp = timestamp,
            Noise = request.Noise,
            Co2 = request.Co2,
            Temperature = request.Temperature,
            Humidity = request.Humidity
        };

        if (request.Lat.HasValue && request.Lon.HasValue)
            doc.Location = new SensorLocation { Lat = request.Lat.Value, Lon = request.Lon.Value };

        return doc;
    }
}
