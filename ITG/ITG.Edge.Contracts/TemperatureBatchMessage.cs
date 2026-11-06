namespace ITG.Edge.Contracts;

public sealed record TemperatureBatchMessage(
    Guid MessageId,
    string MessageType,
    string LocationId,
    int ControllerId,
    DateTime MeasuredAt,
    Dictionary<string, double> Temperatures);