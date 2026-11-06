using Microsoft.EntityFrameworkCore;
using Npgsql;
using SiloSicaklikIzleme.Domain.DbViewModels;
using SiloSicaklikIzleme.Domain.Interfaces;
using SiloSicaklikIzleme.Domain.Models;
using SiloSicaklikIzleme.Domain.Models.ReportModels;

namespace SiloSicaklikIzleme.Infrastructure.Repositories
{
    public class SiloSicaklikRepository : ISiloSicaklikRepository
    {
        // Şema (FACILITY/DEPOT/...) tüm tesisler için ortak. Her sorgu aşağıdaki
        // kapsam sorgularından (tesis -> depo -> kablo -> sensör) türetilir; böylece
        // depotId/cableId/sensorIds elle verilse bile seçili tesisin dışına çıkılamaz
        // ve filtreler ayrı round-trip yerine tek SQL'de birleşir.
        private readonly SiloSicaklikIzlemeDbContext _context;

        public SiloSicaklikRepository(SiloSicaklikIzlemeDbContext context)
        {
            _context = context;
        }

        // tesisKodu null/boş ise tüm tesisler.
        private IQueryable<Facility> TesisKapsami(string? tesisKodu)
        {
            var query = _context.Facilities.AsNoTracking();
            if (string.IsNullOrWhiteSpace(tesisKodu))
            {
                return query;
            }

            var kod = tesisKodu.Trim();
            return query.Where(x => x.Code == kod);
        }

        private IQueryable<Depot> DepoKapsami(string? tesisKodu, Guid? depotId = null)
            => from depot in _context.Depots.AsNoTracking()
               join facility in TesisKapsami(tesisKodu) on depot.FacilityId equals facility.Id
               where !depotId.HasValue || depot.Id == depotId.Value
               select depot;

        private IQueryable<SensorCable> KabloKapsami(string? tesisKodu, Guid? depotId = null)
            => from cable in _context.SensorCables.AsNoTracking()
               join depot in DepoKapsami(tesisKodu, depotId) on cable.DepotId equals depot.Id
               select cable;

        private IQueryable<TemperatureSensor> SensorKapsami(string? tesisKodu, Guid? depotId = null, Guid? cableId = null)
            => from sensor in _context.TemperatureSensors.AsNoTracking()
               join cable in KabloKapsami(tesisKodu, depotId) on sensor.SensorCableId equals cable.Id
               where !cableId.HasValue || cable.Id == cableId.Value
               select sensor;

        // Bucket raporları için sensör id kapsamı; sensorIds verilmişse tesis/depo
        // kapsamıyla kesiştirilir.
        private IQueryable<Guid> RaporSensorIdleri(string? tesisKodu, Guid? depotId, IReadOnlyCollection<Guid>? sensorIds)
        {
            var idler = SensorKapsami(tesisKodu, depotId).Select(x => x.Id);
            if (sensorIds is { Count: > 0 })
            {
                var secilenler = sensorIds.ToList();
                idler = idler.Where(x => secilenler.Contains(x));
            }

            return idler;
        }

        public Task<List<Facility>> GetTesislerAsync()
            => _context.Facilities.AsNoTracking()
                .OrderBy(x => x.Code)
                .ToListAsync();

        public Task<List<Depot>> GetDepotlarAsync(string? tesisKodu = null)
            => (from depot in _context.Depots.AsNoTracking()
                join facility in TesisKapsami(tesisKodu) on depot.FacilityId equals facility.Id
                orderby facility.Code, depot.DepotNo
                select depot)
                .ToListAsync();

        public Task<List<SensorCable>> GetSensorCablelerAsync(string? tesisKodu = null, Guid? depotId = null)
            => KabloKapsami(tesisKodu, depotId)
                .OrderBy(x => x.CableNo)
                .ToListAsync();

        public Task<List<TemperatureSensor>> GetSensorlerAsync(string? tesisKodu = null, Guid? depotId = null, Guid? cableId = null)
            => SensorKapsami(tesisKodu, depotId, cableId)
                .OrderBy(x => x.SensorNo)
                .ToListAsync();

        public Task<List<SensorLatestViewModel>> GetLatestValuesAsync(string? tesisKodu = null, Guid? depotId = null)
        {
            var query =
                from sensor in _context.TemperatureSensors.AsNoTracking()
                join cable in _context.SensorCables.AsNoTracking() on sensor.SensorCableId equals cable.Id
                join depot in DepoKapsami(tesisKodu, depotId) on cable.DepotId equals depot.Id
                join facility in _context.Facilities.AsNoTracking() on depot.FacilityId equals facility.Id
                join latest in _context.SensorLatestValues.AsNoTracking() on sensor.Id equals latest.SensorId into latestGroup
                from latest in latestGroup.DefaultIfEmpty()
                orderby facility.Code, depot.DepotNo, cable.CableNo, sensor.SensorNo
                select new SensorLatestViewModel
                {
                    SensorId = sensor.Id,
                    SensorNo = sensor.SensorNo,
                    SensorKey = sensor.SensorKey,
                    SensorCableId = cable.Id,
                    CableNo = cable.CableNo,
                    DepotId = depot.Id,
                    TesisKodu = facility.Code,
                    TesisAdi = facility.Name,
                    DepotAdi = depot.Name,
                    DepotNo = depot.DepotNo,
                    Temperature = latest != null ? latest.Temperature : (double?)null,
                    MeasuredAt = latest != null ? latest.MeasuredAt : (DateTime?)null,
                };

            return query.ToListAsync();
        }

        public Task<List<TemperatureHourly>> GetTemperatureHourlyAsync(DateTime baslangic, DateTime bitis, string? tesisKodu = null, Guid? depotId = null, IReadOnlyCollection<Guid>? sensorIds = null)
        {
            var sensorIdleri = RaporSensorIdleri(tesisKodu, depotId, sensorIds);

            return _context.TemperatureHourly.AsNoTracking()
                .Where(x => x.Bucket >= baslangic && x.Bucket < bitis)
                .Where(x => sensorIdleri.Contains(x.SensorId))
                .OrderBy(x => x.SensorId).ThenBy(x => x.Bucket)
                .ToListAsync();
        }

        public Task<List<TemperatureDaily>> GetTemperatureDailyAsync(DateTime baslangic, DateTime bitis, string? tesisKodu = null, Guid? depotId = null, IReadOnlyCollection<Guid>? sensorIds = null)
        {
            var sensorIdleri = RaporSensorIdleri(tesisKodu, depotId, sensorIds);

            return _context.TemperatureDaily.AsNoTracking()
                .Where(x => x.Bucket >= baslangic && x.Bucket < bitis)
                .Where(x => sensorIdleri.Contains(x.SensorId))
                .OrderBy(x => x.SensorId).ThenBy(x => x.Bucket)
                .ToListAsync();
        }

        // Depo bazlı kurallar ve kapsamdaki sensörlere tanımlı sensör bazlı kurallar.
        public Task<List<AlarmRule>> GetAlarmRulesAsync(string? tesisKodu = null, Guid? depotId = null)
        {
            var depoIdleri = DepoKapsami(tesisKodu, depotId).Select(x => x.Id);
            var sensorIdleri = SensorKapsami(tesisKodu, depotId).Select(x => x.Id);

            return _context.AlarmRules.AsNoTracking()
                .Where(x =>
                    (x.DepotId.HasValue && depoIdleri.Contains(x.DepotId.Value)) ||
                    (x.SensorId.HasValue && sensorIdleri.Contains(x.SensorId.Value)))
                .ToListAsync();
        }

        // ALARM_EVENT tablosunda bir alarmın "bittiğini" gösteren bir alan yok (sadece
        // StartedAt var) - bu yüzden burada "aktif" yerine en güncel tetiklenen olaylar
        // döndürülüyor. Şemaya bir bitiş/çözülme alanı eklenirse bu metod güncellenmeli.
        public Task<List<AktifAlarmViewModel>> GetActiveAlarmEventsAsync(string? tesisKodu = null, Guid? depotId = null)
        {
            var query =
                from alarmEvent in _context.AlarmEvents.AsNoTracking()
                join rule in _context.AlarmRules.AsNoTracking() on alarmEvent.AlarmRuleId equals rule.Id
                join sensor in _context.TemperatureSensors.AsNoTracking() on alarmEvent.SensorId equals sensor.Id
                join cable in _context.SensorCables.AsNoTracking() on sensor.SensorCableId equals cable.Id
                join depot in DepoKapsami(tesisKodu, depotId) on cable.DepotId equals depot.Id
                join facility in _context.Facilities.AsNoTracking() on depot.FacilityId equals facility.Id
                orderby alarmEvent.StartedAt descending
                select new AktifAlarmViewModel
                {
                    AlarmEventId = alarmEvent.Id,
                    AlarmRuleId = rule.Id,
                    SensorId = sensor.Id,
                    SensorKey = sensor.SensorKey,
                    CableNo = cable.CableNo,
                    DepotId = depot.Id,
                    TesisKodu = facility.Code,
                    TesisAdi = facility.Name,
                    DepotAdi = depot.Name,
                    StartedAt = alarmEvent.StartedAt,
                    WarningThreshold = rule.WarningThreshold,
                    CriticalThreshold = rule.CriticalThreshold,
                };

            return query.ToListAsync();
        }

        // DbContext'in yönettiği aynı bağlantı üzerinden, ON CONFLICT ile tek
        // transaction'da yazılır (bkz. ItgTemperatureIngestionWorker.TemperatureIngestRepository
        // ile aynı desen) - EF Core'un SaveChanges'i native upsert desteklemediği için.
        public async Task InsertReadingsAndUpsertLatestAsync(IReadOnlyCollection<TemperatureReading> readings, CancellationToken cancellationToken = default)
        {
            if (readings.Count == 0)
            {
                return;
            }

            var connection = (NpgsqlConnection)_context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

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
}
