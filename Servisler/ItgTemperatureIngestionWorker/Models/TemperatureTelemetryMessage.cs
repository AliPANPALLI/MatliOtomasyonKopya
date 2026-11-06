namespace ItgTemperatureIngestionWorker.Models;

public sealed class TemperatureTelemetryMessage
{
    public Guid MessageId { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public int ControllerId { get; set; }
    public DateTime MeasuredAt { get; set; }
    public Dictionary<string, double> Temperatures { get; set; } = new();
}
