using System.ComponentModel.DataAnnotations;
using SiloSicaklikIzleme.Application.Interfaces;
using SiloSicaklikIzleme.Domain.DbViewModels;
using SiloSicaklikIzleme.Domain.Interfaces;
using SiloSicaklikIzleme.Domain.Models;
using SiloSicaklikIzleme.Domain.Models.ReportModels;

namespace SiloSicaklikIzleme.Application.Services
{
    public class SiloSicaklikRaporAppService : ISiloSicaklikRaporAppService
    {
        // Özet dilim, seçilen aralığı gün/ay sınırına hizalamadan tam olarak
        // [baslangic, bitis) aralığını, sensör başına tek satıra indirger
        // (bkz. HegSogutmaRaporAppService.OzetSatirlariOlustur ile aynı amaç).

        // Tek istekte okunabilecek en fazla bucket satırı (sensör x dilim). Örn. 936 sensörlük
        // bir tesis için saatlik raporda ~8 gün, günlük raporda ~7 ay.
        private const long MaxRaporSatiri = 200_000;

        private readonly ISiloSicaklikRepository _repository;

        public SiloSicaklikRaporAppService(ISiloSicaklikRepository repository)
        {
            _repository = repository;
        }

        public Task<List<Facility>> GetTesislerAsync() => _repository.GetTesislerAsync();

        public Task<List<Depot>> GetDepolarAsync(string? tesisKodu = null) => _repository.GetDepotlarAsync(tesisKodu);

        public Task<List<SensorCable>> GetKablolarAsync(string? tesisKodu = null, Guid? depotId = null)
            => _repository.GetSensorCablelerAsync(tesisKodu, depotId);

        public Task<List<TemperatureSensor>> GetSensorlerAsync(string? tesisKodu = null, Guid? cableId = null)
            => _repository.GetSensorlerAsync(tesisKodu, cableId: cableId);

        public Task<List<TemperatureSensor>> GetSensorlerByDepotAsync(Guid depotId, string? tesisKodu = null)
            => _repository.GetSensorlerAsync(tesisKodu, depotId: depotId);

        public Task<List<SensorLatestViewModel>> GetAnlikDegerlerAsync(string? tesisKodu = null, Guid? depotId = null)
            => _repository.GetLatestValuesAsync(tesisKodu, depotId);

        public Task<List<AlarmRule>> GetAlarmKurallariAsync(string? tesisKodu = null, Guid? depotId = null)
            => _repository.GetAlarmRulesAsync(tesisKodu, depotId);

        public Task<List<AktifAlarmViewModel>> GetAktifAlarmlarAsync(string? tesisKodu = null, Guid? depotId = null)
            => _repository.GetActiveAlarmEventsAsync(tesisKodu, depotId);

        public async Task<List<SicaklikRaporSatiri>> GetSicaklikRaporuAsync(
            DateTime baslangic,
            DateTime bitis,
            string periyot,
            string? tesisKodu = null,
            Guid? depotId = null,
            IReadOnlyCollection<Guid>? sensorIds = null)
        {
            if (bitis <= baslangic)
            {
                throw new ValidationException("Bitiş tarihi başlangıçtan sonra olmalıdır.");
            }

            var normalizePeriyot = string.IsNullOrWhiteSpace(periyot) ? "saatlik" : periyot.Trim().ToLowerInvariant();
            if (normalizePeriyot is not ("saatlik" or "gunluk" or "ozet"))
            {
                throw new ValidationException("Geçersiz periyot. 'saatlik', 'gunluk' veya 'ozet' olmalıdır.");
            }

            // Repository, sensör kapsamını tesis/depo filtresiyle kesiştirir; burada
            // aynı kapsamın tesis/depo/kablo bilgisi satırlara eklenmek için alınır.
            var sensorBilgisi = (await _repository.GetLatestValuesAsync(tesisKodu, depotId))
                .ToDictionary(x => x.SensorId);

            var sensorSayisi = sensorIds is { Count: > 0 }
                ? sensorIds.Distinct().Count(sensorBilgisi.ContainsKey)
                : sensorBilgisi.Count;

            if (sensorSayisi == 0)
            {
                return new List<SicaklikRaporSatiri>();
            }

            RaporBoyutunuDogrula(baslangic, bitis, normalizePeriyot, sensorSayisi);

            if (normalizePeriyot == "gunluk")
            {
                var gunlukBucketlar = await _repository.GetTemperatureDailyAsync(baslangic, bitis, tesisKodu, depotId, sensorIds);
                return BucketlariSatiraCevir(gunlukBucketlar, sensorBilgisi);
            }

            if (normalizePeriyot == "ozet")
            {
                var saatlikBucketlar = await _repository.GetTemperatureHourlyAsync(baslangic, bitis, tesisKodu, depotId, sensorIds);
                return OzetSatirlariOlustur(saatlikBucketlar, baslangic, sensorBilgisi);
            }

            // varsayılan: saatlik
            var saatlik = await _repository.GetTemperatureHourlyAsync(baslangic, bitis, tesisKodu, depotId, sensorIds);
            return BucketlariSatiraCevir(saatlik, sensorBilgisi);
        }

        // Okunacak bucket satırı = sensör sayısı x dilim sayısı (gunluk: gün, saatlik/ozet: saat;
        // ozet de saatlik bucketları belleğe alıp topladığı için saat üzerinden sayılır).
        private static void RaporBoyutunuDogrula(DateTime baslangic, DateTime bitis, string periyot, int sensorSayisi)
        {
            var aralik = bitis - baslangic;
            var gunluk = periyot == "gunluk";
            var dilimSayisi = (long)Math.Ceiling(gunluk ? aralik.TotalDays : aralik.TotalHours);
            var tahminiSatir = sensorSayisi * dilimSayisi;

            if (tahminiSatir > MaxRaporSatiri)
            {
                throw new ValidationException(
                    $"Seçilen aralık çok geniş: {sensorSayisi} sensör x {dilimSayisi} {(gunluk ? "gün" : "saat")} " +
                    $"= {tahminiSatir:N0} satır (üst sınır {MaxRaporSatiri:N0}). " +
                    "Tarih aralığını daraltın, depo/sensör seçin veya günlük periyodu kullanın.");
            }
        }

        private static List<SicaklikRaporSatiri> BucketlariSatiraCevir(
            List<TemperatureBucketBase> bucketlar,
            Dictionary<Guid, SensorLatestViewModel> sensorBilgisi)
        {
            return bucketlar
                .Select(b => SatirOlustur(b.SensorId, b.Bucket, b.AvgTemperature, b.MinTemperature, b.MaxTemperature, b.ReadingCount, sensorBilgisi))
                .OrderBy(x => x.TesisKodu)
                .ThenBy(x => x.DepotAdi)
                .ThenBy(x => x.CableNo)
                .ThenBy(x => x.SensorKey)
                .ThenBy(x => x.Bucket)
                .ToList();
        }

        private static List<SicaklikRaporSatiri> BucketlariSatiraCevir(
            List<TemperatureDaily> bucketlar,
            Dictionary<Guid, SensorLatestViewModel> sensorBilgisi)
            => BucketlariSatiraCevir(bucketlar.Cast<TemperatureBucketBase>().ToList(), sensorBilgisi);

        private static List<SicaklikRaporSatiri> BucketlariSatiraCevir(
            List<TemperatureHourly> bucketlar,
            Dictionary<Guid, SensorLatestViewModel> sensorBilgisi)
            => BucketlariSatiraCevir(bucketlar.Cast<TemperatureBucketBase>().ToList(), sensorBilgisi);

        // Seçilen aralıktaki saatlik bucketları sensör başına tek satıra indirger:
        // örnek sayısı toplanır, ortalama sıcaklık örnek-ağırlıklı hesaplanır,
        // min/maks aralığın tamamından alınır.
        private static List<SicaklikRaporSatiri> OzetSatirlariOlustur(
            List<TemperatureHourly> saatlikBucketlar,
            DateTime baslangic,
            Dictionary<Guid, SensorLatestViewModel> sensorBilgisi)
        {
            return saatlikBucketlar
                .Cast<TemperatureBucketBase>()
                .GroupBy(x => x.SensorId)
                .Select(g =>
                {
                    var siraliListe = g.OrderBy(x => x.Bucket).ToList();

                    var ornekSayisiToplam = siraliListe.Any(x => x.ReadingCount.HasValue)
                        ? siraliListe.Sum(x => x.ReadingCount ?? 0)
                        : (long?)null;

                    var agirlikliPay = 0d;
                    var agirlikliPayda = 0d;
                    foreach (var b in siraliListe)
                    {
                        if (b.AvgTemperature is null || b.ReadingCount is not > 0) continue;
                        agirlikliPay += b.AvgTemperature.Value * b.ReadingCount.Value;
                        agirlikliPayda += b.ReadingCount.Value;
                    }

                    var avgDegerleri = siraliListe.Where(x => x.AvgTemperature.HasValue).Select(x => x.AvgTemperature!.Value).ToList();
                    var minDegerleri = siraliListe.Where(x => x.MinTemperature.HasValue).Select(x => x.MinTemperature!.Value).ToList();
                    var maksDegerleri = siraliListe.Where(x => x.MaxTemperature.HasValue).Select(x => x.MaxTemperature!.Value).ToList();

                    double? ortSicaklik = agirlikliPayda > 0
                        ? Math.Round(agirlikliPay / agirlikliPayda, 2)
                        : (avgDegerleri.Count > 0 ? Math.Round(avgDegerleri.Average(), 2) : null);

                    return SatirOlustur(
                        g.Key,
                        baslangic,
                        ortSicaklik,
                        minDegerleri.Count > 0 ? minDegerleri.Min() : (double?)null,
                        maksDegerleri.Count > 0 ? maksDegerleri.Max() : (double?)null,
                        ornekSayisiToplam,
                        sensorBilgisi);
                })
                .OrderBy(x => x.TesisKodu)
                .ThenBy(x => x.DepotAdi)
                .ThenBy(x => x.CableNo)
                .ThenBy(x => x.SensorKey)
                .ToList();
        }

        private static SicaklikRaporSatiri SatirOlustur(
            Guid sensorId,
            DateTime bucket,
            double? ortSicaklik,
            double? minSicaklik,
            double? maksSicaklik,
            long? ornekSayisi,
            Dictionary<Guid, SensorLatestViewModel> sensorBilgisi)
        {
            sensorBilgisi.TryGetValue(sensorId, out var bilgi);

            return new SicaklikRaporSatiri
            {
                SensorId = sensorId,
                SensorKey = bilgi?.SensorKey,
                CableNo = bilgi?.CableNo,
                DepotId = bilgi?.DepotId,
                TesisKodu = bilgi?.TesisKodu,
                TesisAdi = bilgi?.TesisAdi,
                DepotAdi = bilgi?.DepotAdi,
                Bucket = bucket,
                OrtSicaklik = ortSicaklik,
                MinSicaklik = minSicaklik,
                MaksSicaklik = maksSicaklik,
                OrnekSayisi = ornekSayisi,
            };
        }
    }
}
