namespace ITG.Edge.Contracts;
public sealed record SensorReading(int ControllerId, DateTime LocalMeasuredAt, string SensorKey, double Temperature);
