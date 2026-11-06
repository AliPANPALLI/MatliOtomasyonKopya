using SiloSicaklikIzleme.Domain.Models;
using SiloSicaklikIzleme.Domain.Models.ReportModels;

namespace SiloSicaklikIzleme.Application.Interfaces
{
    // tesisKodu: FACILITY.Code (örn. "OSB-YEM"); null/boş ise tüm tesisler.
    public interface ISiloSicaklikRaporAppService
    {
        Task<List<Facility>> GetTesislerAsync();
        Task<List<Depot>> GetDepolarAsync(string? tesisKodu = null);
        Task<List<SensorCable>> GetKablolarAsync(string? tesisKodu = null, Guid? depotId = null);
        Task<List<TemperatureSensor>> GetSensorlerAsync(string? tesisKodu = null, Guid? cableId = null);
        Task<List<TemperatureSensor>> GetSensorlerByDepotAsync(Guid depotId, string? tesisKodu = null);
        Task<List<SensorLatestViewModel>> GetAnlikDegerlerAsync(string? tesisKodu = null, Guid? depotId = null);

        // periyot: "saatlik" | "gunluk" | "ozet"
        Task<List<SicaklikRaporSatiri>> GetSicaklikRaporuAsync(DateTime baslangic, DateTime bitis, string periyot, string? tesisKodu = null, Guid? depotId = null, IReadOnlyCollection<Guid>? sensorIds = null);

        Task<List<AlarmRule>> GetAlarmKurallariAsync(string? tesisKodu = null, Guid? depotId = null);
        Task<List<AktifAlarmViewModel>> GetAktifAlarmlarAsync(string? tesisKodu = null, Guid? depotId = null);
    }
}
