using Microsoft.Extensions.Options;
using Npgsql;

namespace HegCoolingAlarm.Worker;

public sealed class AlarmEvaluationWorker : BackgroundService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly HegAlarmWorkerOptions _options;
    private readonly ILogger<AlarmEvaluationWorker> _logger;

    public AlarmEvaluationWorker(
        NpgsqlDataSource dataSource,
        IOptions<HegAlarmWorkerOptions> options,
        ILogger<AlarmEvaluationWorker> logger)
    {
        _dataSource = dataSource;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.EvaluationIntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                await EvaluateAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "HEG alarm evaluation cycle failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task EvaluateAsync(CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT * FROM public.heg_alarm_kontrol_et(now(), @max_measurement_age_seconds)");
        command.Parameters.AddWithValue("max_measurement_age_seconds", Math.Max(1, _options.AlarmMaxMeasurementAgeSeconds));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return;

        _logger.LogInformation(
            "HEG alarm evaluation completed. Processed={Processed}, Opened={Opened}, Closed={Closed}, NotificationsQueued={NotificationsQueued}",
            GetInt64(reader, "islenen"),
            GetInt64(reader, "acilan"),
            GetInt64(reader, "kapanan"),
            GetInt64(reader, "bildirime_eklenen"));
    }

    private static long GetInt64(NpgsqlDataReader reader, string column)
        => Convert.ToInt64(reader.GetValue(reader.GetOrdinal(column)));
}
