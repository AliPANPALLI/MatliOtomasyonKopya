using System.Text.Json;
using HegSogutmaScada.Domain.Models;

namespace HegSogutmaScada.Domain.Interfaces;

public interface IHegAlarmRepository
{
    Task<IReadOnlyList<AlarmEquipmentListItem>> GetEquipmentAsync(string equipmentType, CancellationToken cancellationToken);
    Task<AlarmSettingDto?> GetSettingAsync(string equipmentType, string equipmentId, CancellationToken cancellationToken);
    Task<long> SaveSettingAsync(JsonElement payload, CancellationToken cancellationToken);
    Task<PagedResult<AlarmEventDto>> GetActiveAsync(string? equipmentType, string? equipmentId, int page, int pageSize, CancellationToken cancellationToken);
    Task<PagedResult<AlarmEventDto>> GetHistoryAsync(DateTimeOffset? start, DateTimeOffset? end, string? equipmentType, string? equipmentId, int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlarmNotificationDto>> GetNotificationsAsync(long alarmEventId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlarmMeasurementDto>> GetMeasurementsAsync(string? equipmentType, string? equipmentId, CancellationToken cancellationToken);
    Task<AlarmSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
}
