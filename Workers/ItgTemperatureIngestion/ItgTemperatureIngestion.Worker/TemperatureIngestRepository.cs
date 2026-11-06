using Npgsql;

namespace ItgTemperatureIngestion.Worker;

public sealed record SensorReading(Guid SensorId, DateTime MeasuredAt, double Temperature);

public sealed class TemperatureIngestRepository
{
    private readonly PostgresConnectionFactory _connectionFactory;

    public TemperatureIngestRepository(PostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// TEMPERATURE_READING tablosuna insert, SENSOR_LATEST_VALUE tablosuna upsert atar.
    /// Aynı anda tek transaction içinde çalışır; aynı (SensorId, MeasuredAt) tekrar gelirse
    /// TEMPERATURE_READING atlanır, SENSOR_LATEST_VALUE ise sadece daha yeni ölçümle güncellenir.
    /// </summary>
    public async Task InsertReadingsAndUpsertLatestAsync(
        IReadOnlyCollection<SensorReading> readings,
        CancellationToken cancellationToken = default)
    {
        if (readings.Count == 0)
            return;

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string sql = """
            INSERT INTO "TEMPERATURE_READING" ("SensorId", "MeasuredAt", "Temperature")
            VALUES (@sensorId, @measuredAt, @temperature)
            ON CONFLICT ("SensorId", "MeasuredAt") DO NOTHING;

            INSERT INTO "SENSOR_LATEST_VALUE" ("SensorId", "Temperature", "MeasuredAt")
            VALUES (@sensorId, @temperature, @measuredAt)
            ON CONFLICT ("SensorId") DO UPDATE
            SET "Temperature" = EXCLUDED."Temperature",
                "MeasuredAt" = EXCLUDED."MeasuredAt"
            WHERE EXCLUDED."MeasuredAt" > "SENSOR_LATEST_VALUE"."MeasuredAt";
            """;

        foreach (var reading in readings)
        {
            await using var command = new NpgsqlCommand(sql, connection, transaction);

            command.Parameters.AddWithValue("sensorId", reading.SensorId);
            command.Parameters.AddWithValue("measuredAt", reading.MeasuredAt);
            command.Parameters.AddWithValue("temperature", reading.Temperature);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
