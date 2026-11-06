using System.Text.Json;
using System.Text.Json.Nodes;
using HegSogutmaScada.Application.Interfaces;
using HegSogutmaScada.Domain.Interfaces;
using HegSogutmaScada.Domain.Models;

namespace HegSogutmaScada.Application.Services;

public sealed class HegAlarmAppService : IHegAlarmAppService
{
    private readonly IHegAlarmRepository _repository;

    public HegAlarmAppService(IHegAlarmRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<AlarmEquipmentListItem>> GetEquipmentAsync(string equipmentType, CancellationToken cancellationToken)
        => _repository.GetEquipmentAsync(equipmentType, cancellationToken);

    public Task<AlarmSettingDto?> GetSettingAsync(string equipmentType, string equipmentId, CancellationToken cancellationToken)
        => _repository.GetSettingAsync(equipmentType, equipmentId, cancellationToken);

    public async Task<AlarmSaveResult> SaveSettingAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        var alarmEquipmentId = await _repository.SaveSettingAsync(payload, cancellationToken);
        var root = payload;
        var parameters = TryGetProperty(root, "parameters", out var parametersElement) ? parametersElement : default;

        return new AlarmSaveResult(
            true,
            alarmEquipmentId,
            GetString(root, "equipmentId"),
            GetString(root, "equipmentType"),
            GetBoolean(root, "checkEnabled"),
            GetInt32(parameters, "setupTime"),
            GetInt32(parameters, "loopTime"));
    }

    public async Task<AlarmSaveResult> SaveParametersAsync(string? equipmentId, JsonElement payload, CancellationToken cancellationToken)
    {
        var root = JsonNode.Parse(payload.GetRawText())?.AsObject() ?? new JsonObject();
        var bodyEquipmentId = root["equipmentId"]?.GetValue<string>();
        root["equipmentId"] = string.IsNullOrWhiteSpace(equipmentId) ? bodyEquipmentId : equipmentId;

        if (root["equipmentType"] is null)
            root["equipmentType"] = InferEquipmentType(root["parameters"] as JsonObject);

        using var normalizedPayload = JsonDocument.Parse(root.ToJsonString());
        return await SaveSettingAsync(normalizedPayload.RootElement, cancellationToken);
    }

    public Task<PagedResult<AlarmEventDto>> GetActiveAsync(string? equipmentType, string? equipmentId, int page, int pageSize, CancellationToken cancellationToken)
        => _repository.GetActiveAsync(equipmentType, equipmentId, NormalizePage(page), NormalizePageSize(pageSize), cancellationToken);

    public Task<PagedResult<AlarmEventDto>> GetHistoryAsync(DateTimeOffset? start, DateTimeOffset? end, string? equipmentType, string? equipmentId, int page, int pageSize, CancellationToken cancellationToken)
        => _repository.GetHistoryAsync(start, end, equipmentType, equipmentId, NormalizePage(page), NormalizePageSize(pageSize), cancellationToken);

    public Task<IReadOnlyList<AlarmNotificationDto>> GetNotificationsAsync(long alarmEventId, CancellationToken cancellationToken)
        => _repository.GetNotificationsAsync(alarmEventId, cancellationToken);

    public Task<IReadOnlyList<AlarmMeasurementDto>> GetMeasurementsAsync(string? equipmentType, string? equipmentId, CancellationToken cancellationToken)
        => _repository.GetMeasurementsAsync(equipmentType, equipmentId, cancellationToken);

    public Task<AlarmSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
        => _repository.GetSummaryAsync(cancellationToken);

    private static int NormalizePage(int page) => page < 1 ? 1 : page;
    private static int NormalizePageSize(int pageSize) => Math.Clamp(pageSize, 1, 200);

    private static string InferEquipmentType(JsonObject? parameters)
    {
        if (parameters is not null && parameters.Any(x =>
                x.Key.Equals("temperatureLimit", StringComparison.OrdinalIgnoreCase)
                || x.Key.Equals("temperatureMin", StringComparison.OrdinalIgnoreCase)
                || x.Key.Equals("temperatureMax", StringComparison.OrdinalIgnoreCase)))
        {
            return "ODA";
        }

        return "KOMPRESOR";
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        value = default;
        if (element.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        return false;
    }

    private static string? GetString(JsonElement element, string name)
        => TryGetProperty(element, name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool? GetBoolean(JsonElement element, string name)
        => TryGetProperty(element, name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;

    private static int? GetInt32(JsonElement element, string name)
        => TryGetProperty(element, name, out var value) && value.TryGetInt32(out var parsed) ? parsed : null;
}
