using System.Data;
using System.Globalization;
using Microsoft.Extensions.Options;
using Npgsql;

namespace HegSogutmaAlarmWorker;

public sealed class AlarmNotificationWorker : BackgroundService
{
    private static readonly int[] RetryMinutes = [1, 2, 5, 15, 30];
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly TimeZoneInfo IstanbulTimeZone = ResolveIstanbulTimeZone();

    private readonly NpgsqlDataSource _dataSource;
    private readonly IAlarmNotificationSender _sender;
    private readonly HegAlarmWorkerOptions _options;
    private readonly ILogger<AlarmNotificationWorker> _logger;

    public AlarmNotificationWorker(
        NpgsqlDataSource dataSource,
        IAlarmNotificationSender sender,
        IOptions<HegAlarmWorkerOptions> options,
        ILogger<AlarmNotificationWorker> logger)
    {
        _dataSource = dataSource;
        _sender = sender;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.NotificationIntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                var claimed = await ClaimAsync(stoppingToken);
                foreach (var notification in claimed)
                    await ProcessAsync(notification, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "HEG alarm notification cycle failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task<IReadOnlyList<ClaimedNotification>> ClaimAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            WITH aday AS (
              SELECT id FROM public.alarm_bildirim
              WHERE durum IN ('BEKLIYOR','HATA')
                AND planlanan_zaman <= now()
                AND deneme_sayisi < @maximum_attempts
              ORDER BY planlanan_zaman,id
              FOR UPDATE SKIP LOCKED
              LIMIT @batch_size
            )
            UPDATE public.alarm_bildirim b
            SET durum='ISLENIYOR',
                deneme_sayisi=deneme_sayisi+1,
                hata_mesaji=NULL
            FROM aday
            WHERE b.id=aday.id
            RETURNING b.id::bigint, b.alarm_olay_id::bigint, b.bildirim_turu, b.kanal, b.deneme_sayisi
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("maximum_attempts", Math.Max(1, _options.MaximumDeliveryAttempts));
        command.Parameters.AddWithValue("batch_size", Math.Clamp(_options.NotificationBatchSize, 1, 250));

        var result = new List<ClaimedNotification>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new ClaimedNotification(
                    reader.GetInt64(0),
                    reader.GetInt64(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetInt32(4)));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        if (result.Count > 0)
            _logger.LogInformation("Claimed {Count} HEG alarm notifications.", result.Count);
        return result;
    }

    private async Task ProcessAsync(ClaimedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var detail = await GetDetailAsync(notification.AlarmEventId, cancellationToken)
                ?? throw new InvalidOperationException("Alarm event detail was not found.");

            if (string.Equals(notification.NotificationType, "TEKRAR", StringComparison.OrdinalIgnoreCase)
                && (detail.AlarmEndedAt is not null || !detail.RuleActive || !detail.EquipmentCheckEnabled))
            {
                await MarkCancelledAsync(notification.Id, cancellationToken);
                return;
            }

            var message = BuildMessage(notification.NotificationType, detail);
            await _sender.SendAsync(new AlarmNotificationMessage(notification.Id, notification.Channel, message), cancellationToken);
            await MarkSentAsync(notification.Id, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "HEG notification delivery failed. NotificationId={NotificationId}", notification.Id);
            await MarkFailedAsync(notification, exception, cancellationToken);
        }
    }

    private async Task<AlarmDetail?> GetDetailAsync(long alarmEventId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT ae.equipment_id,
                   ae.check_enabled,
                   r.parametre_kodu,
                   r.yon,
                   r.aktif AS rule_active,
                   ao.ihlal_baslangic_zamani,
                   ao.alarm_baslangic_zamani,
                   ao.alarm_bitis_zamani,
                   ao.son_deger,
                   ao.limit_degeri
            FROM public.alarm_olay ao
            JOIN public.alarm_parametre_kural r ON r.id = ao.kural_id
            JOIN public.alarm_ekipman_ayar ae ON ae.id = r.alarm_ekipman_id
            WHERE ao.id = @alarm_event_id
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("alarm_event_id", alarmEventId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new AlarmDetail(
            reader.GetString(reader.GetOrdinal("equipment_id")),
            reader.GetBoolean(reader.GetOrdinal("check_enabled")),
            reader.GetString(reader.GetOrdinal("parametre_kodu")),
            reader.GetString(reader.GetOrdinal("yon")),
            reader.GetBoolean(reader.GetOrdinal("rule_active")),
            GetDateTimeOffset(reader, "ihlal_baslangic_zamani")!.Value,
            GetDateTimeOffset(reader, "alarm_baslangic_zamani")!.Value,
            GetDateTimeOffset(reader, "alarm_bitis_zamani"),
            GetNullableDecimal(reader, "son_deger"),
            GetDecimal(reader, "limit_degeri"));
    }

    private static string BuildMessage(string notificationType, AlarmDetail detail)
    {
        var direction = detail.Direction == "ALT" ? "alt limit" : "üst limit";
        var parameter = ParameterDisplayName(detail.ParameterCode);
        var unit = IsTemperature(detail.ParameterCode) ? " °C" : string.Empty;
        var lastValue = detail.LastValue?.ToString("0.###", TurkishCulture) ?? "-";
        var limit = detail.Limit.ToString("0.###", TurkishCulture);
        var violationAt = FormatIstanbul(detail.ViolationStartedAt);
        var alarmAt = FormatIstanbul(detail.AlarmStartedAt);

        if (string.Equals(notificationType, "KAPANIS", StringComparison.OrdinalIgnoreCase))
        {
            var endedAt = detail.AlarmEndedAt ?? DateTimeOffset.UtcNow;
            var duration = endedAt - detail.AlarmStartedAt;
            return $"HEG Soğutma | {detail.EquipmentId} | {parameter} {direction} alarmı kapandı | Son ölçüm: {lastValue}{unit} | Limit: {limit}{unit} | Alarm: {alarmAt} | Kapanış: {FormatIstanbul(endedAt)} | Süre: {FormatDuration(duration)}";
        }

        var label = string.Equals(notificationType, "TEKRAR", StringComparison.OrdinalIgnoreCase) ? "TEKRAR" : "AKTIF";
        return $"HEG Soğutma | {detail.EquipmentId} | {parameter} {direction} alarmı | Ölçüm: {lastValue}{unit} | Limit: {limit}{unit} | İhlal: {violationAt} | Alarm: {alarmAt} | Durum: {label}";
    }

    private async Task MarkSentAsync(long id, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand("""
            UPDATE public.alarm_bildirim
            SET durum='GONDERILDI', gonderim_zamani=now(), hata_mesaji=NULL
            WHERE id=@id AND durum='ISLENIYOR'
            """);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task MarkCancelledAsync(long id, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand("""
            UPDATE public.alarm_bildirim
            SET durum='IPTAL', hata_mesaji=NULL
            WHERE id=@id AND durum='ISLENIYOR'
            """);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task MarkFailedAsync(ClaimedNotification notification, Exception exception, CancellationToken cancellationToken)
    {
        var delayIndex = Math.Clamp(notification.AttemptCount - 1, 0, RetryMinutes.Length - 1);
        var summary = $"Delivery failed ({exception.GetType().Name}).";
        await using var command = _dataSource.CreateCommand("""
            UPDATE public.alarm_bildirim
            SET durum='HATA',
                hata_mesaji=@error_message,
                planlanan_zaman=now() + make_interval(mins => @retry_minutes)
            WHERE id=@id AND durum='ISLENIYOR'
            """);
        command.Parameters.AddWithValue("id", notification.Id);
        command.Parameters.AddWithValue("error_message", summary);
        command.Parameters.AddWithValue("retry_minutes", RetryMinutes[delayIndex]);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static decimal? GetNullableDecimal(NpgsqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : Convert.ToDecimal(reader.GetValue(ordinal));
    }

    private static decimal GetDecimal(NpgsqlDataReader reader, string column)
        => Convert.ToDecimal(reader.GetValue(reader.GetOrdinal(column)));

    private static DateTimeOffset? GetDateTimeOffset(NpgsqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        if (reader.IsDBNull(ordinal))
            return null;

        var value = reader.GetDateTime(ordinal);
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private static string ParameterDisplayName(string parameterCode)
    {
        var normalized = parameterCode.Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
        if (normalized.Contains("temperature") || normalized.Contains("sicaklik"))
            return normalized.Contains("discharge") || normalized.Contains("basma") ? "Basma sıcaklığı" : "Sıcaklık";
        if (normalized.Contains("pressure") || normalized.Contains("basinc"))
            return normalized.Contains("oil") || normalized.Contains("yag") ? "Yağ basıncı" : "Basınç";
        if (normalized.Contains("superheat"))
            return "Emiş kızgınlığı";
        return parameterCode;
    }

    private static bool IsTemperature(string parameterCode)
    {
        var normalized = parameterCode.ToLowerInvariant();
        return normalized.Contains("temperature") || normalized.Contains("sicaklik") || normalized.Contains("superheat");
    }

    private static string FormatIstanbul(DateTimeOffset value)
        => TimeZoneInfo.ConvertTime(value, IstanbulTimeZone).ToString("dd.MM.yyyy HH:mm:ss", TurkishCulture);

    private static string FormatDuration(TimeSpan duration)
        => duration.TotalDays >= 1
            ? $"{(int)duration.TotalDays} gün {duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}"
            : $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";

    private static TimeZoneInfo ResolveIstanbulTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        }
    }

    private sealed record ClaimedNotification(long Id, long AlarmEventId, string NotificationType, string Channel, int AttemptCount);

    private sealed record AlarmDetail(
        string EquipmentId,
        bool EquipmentCheckEnabled,
        string ParameterCode,
        string Direction,
        bool RuleActive,
        DateTimeOffset ViolationStartedAt,
        DateTimeOffset AlarmStartedAt,
        DateTimeOffset? AlarmEndedAt,
        decimal? LastValue,
        decimal Limit);
}
