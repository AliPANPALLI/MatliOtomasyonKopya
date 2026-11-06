using SiloTemperatureMonitoring.Domain.DbViewModels;
using SiloTemperatureMonitoring.Domain.Models;
using SiloTemperatureMonitoring.Domain.Models.ReportModels;

namespace SiloTemperatureMonitoring.Domain.Interfaces
{
    // Tüm sorgularda tesisKodu FACILITY.Code ile eşleşir (örn. "OSB-YEM", "MERKEZ-YEM");
    // null/boş ise tüm tesisler kapsama girer. depotId/cableId/sensorIds verilse bile
    // sonuç her zaman seçili tesisin kapsamıyla sınırlanır.
    public interface ISiloSicaklikRepository
    {
        Task<List<Facility>> GetTesislerAsync();
        Task<List<Depot>> GetDepotlarAsync(string? tesisKodu = null);
        Task<List<SensorCable>> GetSensorCablelerAsync(string? tesisKodu = null, Guid? depotId = null);
        Task<List<TemperatureSensor>> GetSensorlerAsync(string? tesisKodu = null, Guid? depotId = null, Guid? cableId = null);

        // FACILITY/DEPOT/SENSOR_CABLE/TEMPERATURE_SENSOR/SENSOR_LATEST_VALUE tek bir LEFT JOIN
        // sorgusuyla birleştirilir (bkz. HegSogutmaRepository.GetVanalarWithDurumAsync
        // ile aynı amaç: ayrı round-trip'ler yerine tek sorgu).
        Task<List<SensorLatestViewModel>> GetLatestValuesAsync(string? tesisKodu = null, Guid? depotId = null);

        Task<List<TemperatureHourly>> GetTemperatureHourlyAsync(DateTime baslangic, DateTime bitis, string? tesisKodu = null, Guid? depotId = null, IReadOnlyCollection<Guid>? sensorIds = null);
        Task<List<TemperatureDaily>> GetTemperatureDailyAsync(DateTime baslangic, DateTime bitis, string? tesisKodu = null, Guid? depotId = null, IReadOnlyCollection<Guid>? sensorIds = null);

        Task<List<AlarmRule>> GetAlarmRulesAsync(string? tesisKodu = null, Guid? depotId = null);
        Task<List<AktifAlarmViewModel>> GetActiveAlarmEventsAsync(string? tesisKodu = null, Guid? depotId = null);

        // Ingestion tarafı: TEMPERATURE_READING'e insert, SENSOR_LATEST_VALUE'ya upsert
        // atar. Aynı (SensorId, MeasuredAt) tekrar gelirse TEMPERATURE_READING atlanır,
        // SENSOR_LATEST_VALUE ise sadece daha yeni ölçümle güncellenir.
        Task InsertReadingsAndUpsertLatestAsync(IReadOnlyCollection<TemperatureReading> readings, CancellationToken cancellationToken = default);
    }
}
