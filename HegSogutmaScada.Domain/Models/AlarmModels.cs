namespace HegSogutmaScada.Domain.Models;

public sealed record AlarmEquipmentListItem(long SourceId, string EquipmentId, string EquipmentType, bool HasAlarmSetting);

public sealed record AlarmSettingDto(
    string EquipmentId,
    IReadOnlyDictionary<string, object?> Parameters,
    bool CheckEnabled);

public sealed record AlarmSaveResult(
    bool Success,
    long AlarmEquipmentId,
    string? EquipmentId,
    string? EquipmentType,
    bool? CheckEnabled,
    int? SetupTimeMinutes,
    int? LoopTimeMinutes);

public sealed record AlarmEventDto(
    long Id,
    string EquipmentType,
    string EquipmentId,
    string ParameterCode,
    string Direction,
    decimal Limit,
    decimal? TriggerValue,
    decimal? LastValue,
    string Status,
    DateTimeOffset ViolationStartedAt,
    DateTimeOffset AlarmStartedAt,
    DateTimeOffset? AlarmEndedAt,
    DateTimeOffset? LastMeasurementAt,
    DateTimeOffset? LastCheckedAt,
    string? CloseReason,
    double DurationSeconds,
    double? MeasurementAgeSeconds);

public sealed record AlarmNotificationDto(
    long Id,
    long AlarmEventId,
    string NotificationType,
    string Channel,
    DateTimeOffset PlannedAt,
    DateTimeOffset? SentAt,
    string Status,
    int AttemptCount,
    string? ErrorMessage,
    DateTimeOffset CreatedAt);

public sealed record AlarmMeasurementDto(
    long MappingId,
    string EquipmentType,
    string EquipmentId,
    string ParameterCode,
    string PlcTag,
    bool Active,
    decimal? Value,
    DateTimeOffset? MeasurementAt,
    DateTimeOffset? ReceivedAt,
    double? MeasurementAgeSeconds);

public sealed record AlarmSummaryDto(int ActiveCount, int WaitingCount, int NoDataCount, int PendingNotificationCount);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount);
