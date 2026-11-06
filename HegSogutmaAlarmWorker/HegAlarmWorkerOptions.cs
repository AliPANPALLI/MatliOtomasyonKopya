namespace HegSogutmaAlarmWorker;

public sealed class HegAlarmWorkerOptions
{
    public const string SectionName = "HegAlarmWorker";

    public int EvaluationIntervalSeconds { get; set; } = 45;
    public int NotificationIntervalSeconds { get; set; } = 10;
    public int AlarmMaxMeasurementAgeSeconds { get; set; } = 180;
    public int NotificationBatchSize { get; set; } = 25;
    public int MaximumDeliveryAttempts { get; set; } = 5;
}
