using System.Data.Common;
using System.Text.Json;
using HegCoolingScada.Domain.Interfaces;
using HegCoolingScada.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace HegCoolingScada.Infrastructure.Repositories;

public sealed class HegAlarmRepository : IHegAlarmRepository
{
    private static readonly string[] CompressorParameterCodes =
    [
        "suctionPressureBar",
        "suctionPressureCr",
        "suctionSuperheat",
        "dischargePressure",
        "dischargeTemperature",
        "oilPressure",
        "oilTemperature"
    ];

    private readonly HegSogutmaScadaDbContext _context;

    public HegAlarmRepository(HegSogutmaScadaDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AlarmEquipmentListItem>> GetEquipmentAsync(string equipmentType, CancellationToken cancellationToken)
    {
        var normalizedType = NormalizeEquipmentType(equipmentType);
        var sql = normalizedType == "KOMPRESOR"
            ? """
              SELECT k.id::bigint AS source_id,
                     k.ad AS equipment_id,
                     'KOMPRESOR'::text AS equipment_type,
                     EXISTS (
                         SELECT 1 FROM public.alarm_ekipman_ayar ae
                         WHERE ae.ekipman_turu = 'KOMPRESOR' AND ae.kaynak_id = k.id
                     ) AS has_alarm_setting
              FROM public.kompresorler k
              ORDER BY k.ad
              """
            : """
              SELECT v.id::bigint AS source_id,
                     v.ekipman_kodu AS equipment_id,
                     'ODA'::text AS equipment_type,
                     EXISTS (
                         SELECT 1 FROM public.alarm_ekipman_ayar ae
                         WHERE ae.ekipman_turu = 'ODA' AND ae.kaynak_id = v.id
                     ) AS has_alarm_setting
              FROM public.oda_vanalari v
              WHERE v.ekipman_kodu IS NOT NULL
              ORDER BY v.ekipman_kodu
              """;

        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = CreateCommand(sql);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var result = new List<AlarmEquipmentListItem>();
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new AlarmEquipmentListItem(
                    reader.GetInt64(reader.GetOrdinal("source_id")),
                    reader.GetString(reader.GetOrdinal("equipment_id")),
                    reader.GetString(reader.GetOrdinal("equipment_type")),
                    reader.GetBoolean(reader.GetOrdinal("has_alarm_setting"))));
            }

            return result;
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    public async Task<AlarmSettingDto?> GetSettingAsync(string equipmentType, string equipmentId, CancellationToken cancellationToken)
    {
        const string settingSql = """
            SELECT ae.id::bigint AS alarm_equipment_id,
                   ae.ekipman_turu,
                   ae.kaynak_id::bigint AS source_id,
                   ae.equipment_id,
                   ae.check_enabled,
                   ae.setup_time_dakika,
                   ae.loop_time_dakika,
                   ae.guncelleme_zamani
            FROM public.alarm_ekipman_ayar ae
            WHERE ae.ekipman_turu = @equipment_type AND ae.equipment_id = @equipment_id
            ORDER BY ae.id
            LIMIT 1
            """;

        const string ruleSql = """
            SELECT r.parametre_kodu, r.yon, r.esik
            FROM public.alarm_parametre_kural r
            WHERE r.alarm_ekipman_id = @alarm_equipment_id
            ORDER BY r.parametre_kodu, r.yon
            """;

        var normalizedType = NormalizeEquipmentType(equipmentType);
        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            long alarmEquipmentId;
            bool checkEnabled;
            int setupTime;
            int loopTime;

            await using (var command = CreateCommand(settingSql))
            {
                command.Parameters.AddWithValue("equipment_type", normalizedType);
                command.Parameters.AddWithValue("equipment_id", equipmentId);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                    return null;

                alarmEquipmentId = reader.GetInt64(reader.GetOrdinal("alarm_equipment_id"));
                checkEnabled = reader.GetBoolean(reader.GetOrdinal("check_enabled"));
                setupTime = reader.GetInt32(reader.GetOrdinal("setup_time_dakika"));
                loopTime = reader.GetInt32(reader.GetOrdinal("loop_time_dakika"));
            }

            var rules = new Dictionary<(string ParameterCode, string Direction), decimal>();
            await using (var command = CreateCommand(ruleSql))
            {
                command.Parameters.AddWithValue("alarm_equipment_id", alarmEquipmentId);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var parameterCode = CanonicalParameterCode(reader.GetString(reader.GetOrdinal("parametre_kodu")));
                    var direction = reader.GetString(reader.GetOrdinal("yon")).ToUpperInvariant();
                    rules[(parameterCode, direction)] = GetDecimal(reader, "esik");
                }
            }

            var parameters = BuildSettingParameters(normalizedType, setupTime, loopTime, rules);
            return new AlarmSettingDto(equipmentId, parameters, checkEnabled);
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    public async Task<long> SaveSettingAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var connection = (NpgsqlConnection)_context.Database.GetDbConnection();
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                "SELECT public.heg_alarm_ayar_kaydet(@payload::jsonb)",
                connection,
                transaction);
            command.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb) { Value = payload.GetRawText() });
            var result = await command.ExecuteScalarAsync(cancellationToken);
            var alarmEquipmentId = Convert.ToInt64(result);

            await SaveCompressorMaximumRulesAsync(
                connection,
                transaction,
                alarmEquipmentId,
                payload,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return alarmEquipmentId;
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    public Task<PagedResult<AlarmEventDto>> GetActiveAsync(string? equipmentType, string? equipmentId, int page, int pageSize, CancellationToken cancellationToken)
        => GetEventsAsync(true, null, null, equipmentType, equipmentId, page, pageSize, cancellationToken);

    public Task<PagedResult<AlarmEventDto>> GetHistoryAsync(DateTimeOffset? start, DateTimeOffset? end, string? equipmentType, string? equipmentId, int page, int pageSize, CancellationToken cancellationToken)
        => GetEventsAsync(false, start, end, equipmentType, equipmentId, page, pageSize, cancellationToken);

    public async Task<IReadOnlyList<AlarmNotificationDto>> GetNotificationsAsync(long alarmEventId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT b.id::bigint AS id,
                   b.alarm_olay_id::bigint AS alarm_event_id,
                   b.bildirim_turu,
                   b.kanal,
                   b.planlanan_zaman,
                   b.gonderim_zamani,
                   b.durum,
                   b.deneme_sayisi,
                   b.hata_mesaji,
                   b.kayit_zamani
            FROM public.alarm_bildirim b
            WHERE b.alarm_olay_id = @alarm_event_id
            ORDER BY b.kayit_zamani, b.id
            """;

        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = CreateCommand(sql);
            command.Parameters.AddWithValue("alarm_event_id", alarmEventId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var result = new List<AlarmNotificationDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new AlarmNotificationDto(
                    reader.GetInt64(reader.GetOrdinal("id")),
                    reader.GetInt64(reader.GetOrdinal("alarm_event_id")),
                    reader.GetString(reader.GetOrdinal("bildirim_turu")),
                    reader.GetString(reader.GetOrdinal("kanal")),
                    GetDateTimeOffset(reader, "planlanan_zaman")!.Value,
                    GetDateTimeOffset(reader, "gonderim_zamani"),
                    reader.GetString(reader.GetOrdinal("durum")),
                    reader.GetInt32(reader.GetOrdinal("deneme_sayisi")),
                    GetNullableString(reader, "hata_mesaji"),
                    GetDateTimeOffset(reader, "kayit_zamani")!.Value));
            }

            return result;
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    public async Task<IReadOnlyList<AlarmMeasurementDto>> GetMeasurementsAsync(string? equipmentType, string? equipmentId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT m.id::bigint AS mapping_id,
                   ae.ekipman_turu,
                   ae.equipment_id,
                   m.parametre_kodu,
                   m.plc_tag,
                   m.aktif,
                   o.deger,
                   o.olcum_zamani,
                   o.alim_zamani,
                   CASE WHEN o.olcum_zamani IS NULL THEN NULL
                        ELSE EXTRACT(EPOCH FROM (now() - o.olcum_zamani))::double precision
                   END AS measurement_age_seconds
            FROM public.alarm_plc_esleme m
            JOIN public.alarm_ekipman_ayar ae ON ae.id = m.alarm_ekipman_id
            LEFT JOIN public.alarm_olcum_anlik o ON o.esleme_id = m.id
            WHERE (@equipment_type IS NULL OR ae.ekipman_turu = @equipment_type)
              AND (@equipment_id IS NULL OR ae.equipment_id = @equipment_id)
            ORDER BY ae.ekipman_turu, ae.equipment_id, m.parametre_kodu
            """;

        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = CreateCommand(sql);
            AddNullableText(command, "equipment_type", NormalizeOptionalEquipmentType(equipmentType));
            AddNullableText(command, "equipment_id", equipmentId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var result = new List<AlarmMeasurementDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new AlarmMeasurementDto(
                    reader.GetInt64(reader.GetOrdinal("mapping_id")),
                    reader.GetString(reader.GetOrdinal("ekipman_turu")),
                    reader.GetString(reader.GetOrdinal("equipment_id")),
                    reader.GetString(reader.GetOrdinal("parametre_kodu")),
                    reader.GetString(reader.GetOrdinal("plc_tag")),
                    reader.GetBoolean(reader.GetOrdinal("aktif")),
                    GetNullableDecimal(reader, "deger"),
                    GetDateTimeOffset(reader, "olcum_zamani"),
                    GetDateTimeOffset(reader, "alim_zamani"),
                    GetNullableDouble(reader, "measurement_age_seconds")));
            }

            return result;
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    public async Task<AlarmSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*) FILTER (WHERE durum = 'AKTIF')::int AS active_count,
                   COUNT(*) FILTER (WHERE durum = 'BEKLIYOR')::int AS waiting_count,
                   COUNT(*) FILTER (WHERE durum = 'VERI_YOK')::int AS no_data_count,
                   (SELECT COUNT(*)::int FROM public.alarm_bildirim WHERE durum IN ('BEKLIYOR', 'HATA')) AS pending_notification_count
            FROM public.alarm_durum
            """;

        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = CreateCommand(sql);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            return new AlarmSummaryDto(
                reader.GetInt32(reader.GetOrdinal("active_count")),
                reader.GetInt32(reader.GetOrdinal("waiting_count")),
                reader.GetInt32(reader.GetOrdinal("no_data_count")),
                reader.GetInt32(reader.GetOrdinal("pending_notification_count")));
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    private async Task<PagedResult<AlarmEventDto>> GetEventsAsync(
        bool activeOnly,
        DateTimeOffset? start,
        DateTimeOffset? end,
        string? equipmentType,
        string? equipmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*) OVER()::bigint AS total_count,
                   ao.id::bigint AS id,
                   ae.ekipman_turu,
                   ae.equipment_id,
                   r.parametre_kodu,
                   r.yon,
                   ao.limit_degeri,
                   ao.tetikleyen_deger,
                   ao.son_deger,
                   COALESCE(d.durum, CASE WHEN ao.alarm_bitis_zamani IS NULL THEN 'AKTIF' ELSE 'NORMAL' END) AS durum,
                   ao.ihlal_baslangic_zamani,
                   ao.alarm_baslangic_zamani,
                   ao.alarm_bitis_zamani,
                   d.son_olcum_zamani,
                   d.son_kontrol_zamani,
                   ao.kapanis_nedeni,
                   EXTRACT(EPOCH FROM (COALESCE(ao.alarm_bitis_zamani, now()) - ao.alarm_baslangic_zamani))::double precision AS duration_seconds,
                   CASE WHEN d.son_olcum_zamani IS NULL THEN NULL
                        ELSE EXTRACT(EPOCH FROM (now() - d.son_olcum_zamani))::double precision
                   END AS measurement_age_seconds
            FROM public.alarm_olay ao
            JOIN public.alarm_parametre_kural r ON r.id = ao.kural_id
            JOIN public.alarm_ekipman_ayar ae ON ae.id = r.alarm_ekipman_id
            LEFT JOIN public.alarm_durum d ON d.kural_id = r.id
            WHERE (@active_only = FALSE OR ao.alarm_bitis_zamani IS NULL)
              AND (@start_time IS NULL OR ao.alarm_baslangic_zamani >= @start_time)
              AND (@end_time IS NULL OR ao.alarm_baslangic_zamani < @end_time)
              AND (@equipment_type IS NULL OR ae.ekipman_turu = @equipment_type)
              AND (@equipment_id IS NULL OR ae.equipment_id = @equipment_id)
            ORDER BY ao.alarm_baslangic_zamani DESC, ao.id DESC
            LIMIT @page_size OFFSET @offset
            """;

        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = CreateCommand(sql);
            command.Parameters.AddWithValue("active_only", activeOnly);
            AddNullableTimestamp(command, "start_time", start);
            AddNullableTimestamp(command, "end_time", end);
            AddNullableText(command, "equipment_type", NormalizeOptionalEquipmentType(equipmentType));
            AddNullableText(command, "equipment_id", equipmentId);
            command.Parameters.AddWithValue("page_size", pageSize);
            command.Parameters.AddWithValue("offset", (page - 1) * pageSize);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var result = new List<AlarmEventDto>();
            long totalCount = 0;
            while (await reader.ReadAsync(cancellationToken))
            {
                totalCount = reader.GetInt64(reader.GetOrdinal("total_count"));
                result.Add(new AlarmEventDto(
                    reader.GetInt64(reader.GetOrdinal("id")),
                    reader.GetString(reader.GetOrdinal("ekipman_turu")),
                    reader.GetString(reader.GetOrdinal("equipment_id")),
                    reader.GetString(reader.GetOrdinal("parametre_kodu")),
                    reader.GetString(reader.GetOrdinal("yon")),
                    GetDecimal(reader, "limit_degeri"),
                    GetNullableDecimal(reader, "tetikleyen_deger"),
                    GetNullableDecimal(reader, "son_deger"),
                    reader.GetString(reader.GetOrdinal("durum")),
                    GetDateTimeOffset(reader, "ihlal_baslangic_zamani")!.Value,
                    GetDateTimeOffset(reader, "alarm_baslangic_zamani")!.Value,
                    GetDateTimeOffset(reader, "alarm_bitis_zamani"),
                    GetDateTimeOffset(reader, "son_olcum_zamani"),
                    GetDateTimeOffset(reader, "son_kontrol_zamani"),
                    GetNullableString(reader, "kapanis_nedeni"),
                    reader.GetDouble(reader.GetOrdinal("duration_seconds")),
                    GetNullableDouble(reader, "measurement_age_seconds")));
            }

            return new PagedResult<AlarmEventDto>(result, page, pageSize, totalCount);
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    private NpgsqlCommand CreateCommand(string sql)
        => new(sql, (NpgsqlConnection)_context.Database.GetDbConnection());

    private static string NormalizeEquipmentType(string equipmentType)
    {
        var normalized = equipmentType.Trim().ToUpperInvariant();
        if (normalized is not ("KOMPRESOR" or "ODA"))
            throw new ArgumentException("Ekipman türü KOMPRESOR veya ODA olmalıdır.", nameof(equipmentType));
        return normalized;
    }

    private static string? NormalizeOptionalEquipmentType(string? equipmentType)
        => string.IsNullOrWhiteSpace(equipmentType) ? null : NormalizeEquipmentType(equipmentType);

    private static IReadOnlyDictionary<string, object?> BuildSettingParameters(
        string equipmentType,
        int setupTime,
        int loopTime,
        IReadOnlyDictionary<(string ParameterCode, string Direction), decimal> rules)
    {
        var parameters = new Dictionary<string, object?>();

        if (equipmentType == "KOMPRESOR")
        {
            foreach (var parameterCode in CompressorParameterCodes)
            {
                parameters[parameterCode] = rules.TryGetValue((parameterCode, "UST"), out var maximum)
                    ? maximum
                    : null;
            }

            parameters["setupTime"] = setupTime;
            parameters["loopTime"] = loopTime;

            foreach (var parameterCode in CompressorParameterCodes)
            {
                if (rules.TryGetValue((parameterCode, "ALT"), out var threshold))
                    parameters[$"{parameterCode}Min"] = threshold;
            }
        }
        else
        {
            parameters["setupTime"] = setupTime;
            parameters["loopTime"] = loopTime;

            if (rules.TryGetValue(("temperature", "ALT"), out var minimum))
                parameters["temperatureMin"] = minimum;

            // Geriye dönük istemci sözleşmesi temperatureLimit alanını her zaman bekliyor.
            parameters["temperatureLimit"] = rules.TryGetValue(("temperature", "UST"), out var maximum)
                ? maximum
                : null;
        }

        return parameters;
    }

    private static async Task SaveCompressorMaximumRulesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long alarmEquipmentId,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        if (!TryGetJsonProperty(payload, "equipmentType", out var equipmentTypeElement)
            || !string.Equals(equipmentTypeElement.GetString(), "KOMPRESOR", StringComparison.OrdinalIgnoreCase)
            || !TryGetJsonProperty(payload, "parameters", out var parametersElement)
            || parametersElement.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        await using (var deactivateCommand = new NpgsqlCommand("""
            UPDATE public.alarm_parametre_kural
            SET aktif = FALSE
            WHERE alarm_ekipman_id = @alarm_equipment_id
              AND yon = 'UST'
            """, connection, transaction))
        {
            deactivateCommand.Parameters.AddWithValue("alarm_equipment_id", alarmEquipmentId);
            await deactivateCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var parameterCode in CompressorParameterCodes)
        {
            if (!TryGetJsonProperty(parametersElement, parameterCode, out var thresholdElement)
                || thresholdElement.ValueKind != JsonValueKind.Number
                || !thresholdElement.TryGetDouble(out var threshold))
            {
                continue;
            }

            await using var updateCommand = new NpgsqlCommand("""
                UPDATE public.alarm_parametre_kural
                SET esik = @threshold, aktif = TRUE
                WHERE alarm_ekipman_id = @alarm_equipment_id
                  AND parametre_kodu = @parameter_code
                  AND yon = 'UST'
                """, connection, transaction);
            updateCommand.Parameters.AddWithValue("threshold", threshold);
            updateCommand.Parameters.AddWithValue("alarm_equipment_id", alarmEquipmentId);
            updateCommand.Parameters.AddWithValue("parameter_code", parameterCode);
            var updated = await updateCommand.ExecuteNonQueryAsync(cancellationToken);

            if (updated > 0)
                continue;

            await using var insertCommand = new NpgsqlCommand("""
                INSERT INTO public.alarm_parametre_kural
                    (alarm_ekipman_id, parametre_kodu, yon, esik, aktif)
                VALUES
                    (@alarm_equipment_id, @parameter_code, 'UST', @threshold, TRUE)
                """, connection, transaction);
            insertCommand.Parameters.AddWithValue("alarm_equipment_id", alarmEquipmentId);
            insertCommand.Parameters.AddWithValue("parameter_code", parameterCode);
            insertCommand.Parameters.AddWithValue("threshold", threshold);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static bool TryGetJsonProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;
        if (element.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        return false;
    }

    private static string CanonicalParameterCode(string parameterCode)
    {
        var normalized = parameterCode
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        return normalized switch
        {
            "suctionpressurebar" or "suctionpressurebarmin" => "suctionPressureBar",
            "suctionpressurecr" or "suctionpressurecrmin" => "suctionPressureCr",
            "suctionsuperheat" or "suctionsuperheatmin" => "suctionSuperheat",
            "dischargepressure" or "dischargepressuremin" => "dischargePressure",
            "dischargetemperature" or "dischargetemperaturemin" => "dischargeTemperature",
            "oilpressure" or "oilpressuremin" => "oilPressure",
            "oiltemperature" or "oiltemperaturemin" => "oilTemperature",
            "temperature" or "temperaturemin" or "temperaturemax" or "temperaturelimit"
                or "sicaklik" or "sicaklikmin" or "sicaklikmax" => "temperature",
            _ => parameterCode
        };
    }

    private static void AddNullableText(NpgsqlCommand command, string name, string? value)
        => command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Text) { Value = value ?? (object)DBNull.Value });

    private static void AddNullableTimestamp(NpgsqlCommand command, string name, DateTimeOffset? value)
        => command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.TimestampTz) { Value = value?.UtcDateTime ?? (object)DBNull.Value });

    private static string? GetNullableString(DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static decimal? GetNullableDecimal(DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : Convert.ToDecimal(reader.GetValue(ordinal));
    }

    private static decimal GetDecimal(DbDataReader reader, string name)
        => Convert.ToDecimal(reader.GetValue(reader.GetOrdinal(name)));

    private static double? GetNullableDouble(DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetDouble(ordinal);
    }

    private static DateTimeOffset? GetDateTimeOffset(DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        if (reader.IsDBNull(ordinal))
            return null;

        var value = reader.GetDateTime(ordinal);
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
