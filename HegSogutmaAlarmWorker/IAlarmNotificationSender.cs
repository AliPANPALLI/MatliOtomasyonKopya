namespace HegSogutmaAlarmWorker;

public sealed record AlarmNotificationMessage(long NotificationId, string Channel, string Text);

public interface IAlarmNotificationSender
{
    Task SendAsync(AlarmNotificationMessage message, CancellationToken cancellationToken);
}

public sealed class SystemAlarmNotificationSender : IAlarmNotificationSender
{
    private readonly ILogger<SystemAlarmNotificationSender> _logger;

    public SystemAlarmNotificationSender(ILogger<SystemAlarmNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(AlarmNotificationMessage message, CancellationToken cancellationToken)
    {
        if (!string.Equals(message.Channel, "SISTEM", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"Unsupported HEG alarm notification channel: {message.Channel}");

        _logger.LogWarning(
            "HEG system alarm notification delivered. NotificationId={NotificationId}, IdempotencyKey={IdempotencyKey}, Message={Message}",
            message.NotificationId,
            $"heg-alarm-{message.NotificationId}",
            message.Text);

        return Task.CompletedTask;
    }
}
