using System.Collections.Concurrent;
using Npgsql;
using ItgTemperatureIngestion.Infrastructure.Persistence;

namespace ItgTemperatureIngestion.Infrastructure.Caching;

/// <summary>
/// FACILITY.Code + ITG_CONTROLLER.Code + "{Depot}_{Kablo}_{Sensor}" anahtarından
/// TEMPERATURE_SENSOR.Id'ye giden eşlemeyi bellekte tutar.
/// </summary>
public sealed class SensorCache
{
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<SensorCache> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private ConcurrentDictionary<string, Guid> _map = new();
    private DateTime _lastRefreshUtc = DateTime.MinValue;

    public SensorCache(
        PostgresConnectionFactory connectionFactory,
        ILogger<SensorCache> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public bool TryGetSensorId(string facilityCode, string controllerCode, string fullSensorKey, out Guid sensorId)
    {
        return _map.TryGetValue(BuildKey(facilityCode, controllerCode, fullSensorKey), out sensorId);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken, bool force = false)
    {
        if (!force && DateTime.UtcNow - _lastRefreshUtc < TimeSpan.FromSeconds(30))
            return;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (!force && DateTime.UtcNow - _lastRefreshUtc < TimeSpan.FromSeconds(30))
                return;

            const string sql = """
                SELECT f."Code" AS "FacilityCode",
                       ic."Code" AS "ControllerCode",
                       d."Name" || '_' || sc."CableNo" || '_' || ts."SensorKey" AS "FullKey",
                       ts."Id" AS "SensorId"
                FROM "TEMPERATURE_SENSOR" ts
                JOIN "SENSOR_CABLE" sc ON sc."Id" = ts."SensorCableId"
                JOIN "DEPOT" d ON d."Id" = sc."DepotId"
                JOIN "ITG_CONTROLLER" ic ON ic."Id" = sc."ControllerId"
                JOIN "FACILITY" f ON f."Id" = ic."FacilityId"
                """;

            var map = new ConcurrentDictionary<string, Guid>();

            await using var connection = _connectionFactory.Create();
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var facilityCode = reader.GetString(0);
                var controllerCode = reader.GetString(1);
                var fullKey = reader.GetString(2);
                var sensorId = reader.GetGuid(3);

                map[BuildKey(facilityCode, controllerCode, fullKey)] = sensorId;
            }

            _map = map;
            _lastRefreshUtc = DateTime.UtcNow;

            _logger.LogInformation("Sensör eşleme tablosu yenilendi. {Count} sensör yüklendi.", map.Count);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static string BuildKey(string facilityCode, string controllerCode, string fullSensorKey)
        => $"{facilityCode}|{controllerCode}|{fullSensorKey}";
}
