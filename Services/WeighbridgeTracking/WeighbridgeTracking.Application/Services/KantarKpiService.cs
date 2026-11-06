
using WeighbridgeTracking.Application.DTOs;
using WeighbridgeTracking.Application.Interfaces;
using WeighbridgeTracking.Domain.Exceptions;
using WeighbridgeTracking.Domain.Helpers;
using WeighbridgeTracking.Domain.Interfaces;
using WeighbridgeTracking.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WeighbridgeTracking.Application.Services
{
    public sealed class KantarKpiService : IKantarKpiService
    {
        private readonly IKantarTakipRepository _repository;
        private readonly ISapKantarService _sap;

        // Gerçek çıkış kaydı olmadığı için otomatik eşleştirmede
        // kullanılan geçici maksimum ziyaret süresi.
        // İşletmenin operasyon kurallarına göre değiştirilebilir.
        private const int MaxVisitHours = 24;

        #region Durum Sabitleri

        private const string NumuneBekliyor = "NUMUNE_BEKLIYOR";
        private const string KantarBekliyor = "KANTAR_BEKLIYOR";
        private const string TartimDevamEdiyor = "TARTIM_DEVAM_EDIYOR";
        private const string KismiTamamlandi = "KISMI_TAMAMLANDI";
        private const string Tamamlandi = "TAMAMLANDI";
        private const string GecmisEksikKayit = "GECMIS_EKSIK_KAYIT";

        private const string Eslesmedi = "ESLESMEDI";
        private const string KontrolGerekli = "KONTROL_GEREKLI";
        private const string CokluFisKontrol = "COKLU_FIS_KONTROL";
        private const string ZamanEslesti = "ZAMAN_ESLESTI";

        #endregion

        public KantarKpiService(
            IKantarTakipRepository repository,
            ISapKantarService sap)
        {
            _repository = repository;
            _sap = sap;
        }

        private sealed record Ziyaret(
            int Id,
            string? ZiyaretciKayitId,
            string? Plaka,
            string NormalizedPlaka,
            int? FirmaId,
            string? FirmaAdi,
            DateTime Giris);

        public async Task<KantarKpiRaporDto> GetRaporAsync(
            DateOnly startDate,
            DateOnly endDate,
            int? kantarId = null,
            string? plaka = null,
            int? firmaId = null,
            CancellationToken cancellationToken = default)
        {
            if (endDate < startDate ||
                endDate.DayNumber - startDate.DayNumber > 6)
            {
                throw new GecersizIstekException(
                    "En fazla 7 günlük tarih aralığı sorgulanabilir.");
            }

            if (kantarId is <= 0 || firmaId is <= 0)
            {
                throw new GecersizIstekException(
                    "Kantar ve firma ID pozitif olmalıdır.");
            }

            var now = DateTime.Now;

            var begin = startDate.ToDateTime(TimeOnly.MinValue);

            var finishExclusive = endDate
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue);

            var plateFilter = PlakaHelper.Normalize(plaka);

            // Rapor aralığı + 2 gün: aynı plakanın sonraki girişlerini
            // bulmak için. Böylece önceki ziyaretin SAP fişleri
            // sonraki ziyarete taşınmaz.
            var allEntries = (await _repository
                .GetKantarGuvenlikByKayitAraligi(
                    begin,
                    finishExclusive.AddDays(2),
                    cancellationToken))
                .Select(ToZiyaret)
                .ToList();

            var visits = allEntries
                .Where(x =>
                    x.Giris < finishExclusive &&
                    (plateFilter.Length == 0 || x.NormalizedPlaka == plateFilter) &&
                    (!firmaId.HasValue || x.FirmaId == firmaId.Value))
                .ToList();

            var sapRows = await LoadSapRowsAsync(
                startDate, endDate, now, kantarId, plateFilter, cancellationToken);

            var sapByPlate = sapRows
                .ToLookup(x => PlakaHelper.Normalize(x.Plaka));

            var entryGroups = BuildEntryGroups(allEntries, visits);

            var samples = await LoadSamplesAsync(visits, cancellationToken);

            var report = new KantarKpiRaporDto
            {
                Baslangic = startDate,
                Bitis = endDate,
                HesaplamaZamani = now,
                KantarId = kantarId
            };

            var assigned = new HashSet<string>();

            foreach (var visit in visits
                .OrderBy(x => x.Giris)
                .ThenBy(x => x.Id))
            {
                report.Ziyaretler.Add(BuildVisitKpi(
                    visit, sapByPlate, entryGroups, samples, now, assigned));
            }

            FillGeneralKpi(report, sapRows, assigned);

            report.Firmalar = BuildFirmaKpis(report.Ziyaretler);

            AddGeneralWarnings(report, kantarId.HasValue);

            return report;
        }

        #region Veri Yükleme

        private static Ziyaret ToZiyaret(KantarGuvenlik g)
        {
            return new Ziyaret(
                g.Id,
                g.ZiyaretciKayitId,
                g.Plaka,
                PlakaHelper.Normalize(g.Plaka),
                g.FirmaId,
                g.Firma?.Name,
                g.KayitZamani!.Value);
        }

        private async Task<List<SapKantarDto>> LoadSapRowsAsync(
            DateOnly startDate,
            DateOnly endDate,
            DateTime now,
            int? kantarId,
            string plateFilter,
            CancellationToken cancellationToken)
        {
            // SapKantarService listeyi önbellekte tuttuğu için
            // kopyası üzerinde çalışılır.
            var rows = new List<SapKantarDto>(
                await _sap.GetByDateAsync(
                    startDate, endDate, cancellationToken));

            // Geçmiş bir raporun son günündeki ziyaretin
            // ertesi güne taşan SAP tartımlarını yakala.
            if (endDate < DateOnly.FromDateTime(now) &&
                endDate < DateOnly.MaxValue)
            {
                var nextDay = endDate.AddDays(1);

                rows.AddRange(await _sap.GetByDateAsync(
                    nextDay, nextDay, cancellationToken));
            }

            return rows
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.FisNo) &&
                    (!kantarId.HasValue || x.KantarId == kantarId.Value) &&
                    (plateFilter.Length == 0 || PlakaHelper.Normalize(x.Plaka) == plateFilter))
                .GroupBy(SapKey)
                .Select(g => g.First())
                .ToList();
        }

        private static Dictionary<string, List<Ziyaret>> BuildEntryGroups(
            List<Ziyaret> allEntries,
            List<Ziyaret> visits)
        {
            var visitedPlates = visits
                .Select(x => x.NormalizedPlaka)
                .Where(x => x.Length > 0)
                .ToHashSet();

            return allEntries
                .Where(x => visitedPlates.Contains(x.NormalizedPlaka))
                .GroupBy(x => x.NormalizedPlaka)
                .ToDictionary(
                    x => x.Key,
                    x => x
                        .OrderBy(v => v.Giris)
                        .ThenBy(v => v.Id)
                        .ToList());
        }

        // Ziyaret başına ilk kalite kaydının zamanı (numune zamanı).
        private async Task<Dictionary<int, DateTime>> LoadSamplesAsync(
            List<Ziyaret> visits,
            CancellationToken cancellationToken)
        {
            var qualityRecords = await _repository
                .GetKantarKaliteByKantarGuvenlikIds(
                    visits.Select(x => x.Id).ToArray(),
                    cancellationToken);

            return qualityRecords
                .Where(k => k.KayitZamani.HasValue)
                .GroupBy(k => k.KantarGuvenlikId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.Min(x => x.KayitZamani!.Value));
        }

        #endregion

        #region Ziyaret KPI

        private static KantarZiyaretKpiDto BuildVisitKpi(
            Ziyaret visit,
            ILookup<string, SapKantarDto> sapByPlate,
            Dictionary<string, List<Ziyaret>> entryGroups,
            Dictionary<int, DateTime> samples,
            DateTime now,
            HashSet<string> assigned)
        {
            var nextEntry = FindNextEntry(visit, entryGroups);

            // Güvenlik girişinden itibaren en fazla
            // 24 saatlik eşleştirme penceresi.
            var maxVisitEnd = visit.Giris.AddHours(MaxVisitHours);

            var matchEnd =
                nextEntry.HasValue && nextEntry.Value < maxVisitEnd
                    ? nextEntry.Value
                    : maxVisitEnd;

            // SAP ilk tartımı güvenlik girişinden
            // önceyse kesinlikle eşleşemez.
            var candidates = visit.NormalizedPlaka.Length == 0
                ? new List<SapKantarDto>()
                : sapByPlate[visit.NormalizedPlaka]
                    .Where(x =>
                        x.FirstAt.HasValue &&
                        x.FirstAt.Value >= visit.Giris &&
                        x.FirstAt.Value < matchEnd)
                    .OrderBy(x => x.FirstAt)
                    .ToList();

            // İkinci tartımı sonraki ziyaretin
            // zamanına taşıyan fişleri otomatik alma.
            var matched = candidates
                .Where(x =>
                    !x.SecondAt.HasValue ||
                    x.SecondAt.Value < matchEnd)
                .ToList();

            var conflictingCount = candidates.Count - matched.Count;

            foreach (var item in matched)
            {
                assigned.Add(SapKey(item));
            }

            var completedWeighings = matched
                .Where(IsCompleted)
                .ToList();

            DateTime? first = matched.FirstOrDefault()?.FirstAt;

            DateTime? second = completedWeighings.Max(x => x.SecondAt);

            DateTime? sample =
                samples.TryGetValue(visit.Id, out var sampleTime)
                    ? sampleTime
                    : null;

            var dto = new KantarZiyaretKpiDto
            {
                GuvenlikId = visit.Id,
                ZiyaretciKayitId = visit.ZiyaretciKayitId,
                Plaka = visit.Plaka,
                FirmaId = visit.FirmaId,
                FirmaAdi = visit.FirmaAdi,

                GirisZamani = visit.Giris,
                NumuneZamani = sample,

                IlkTartimZamani = first,
                SonTartimZamani = second,

                SapFisSayisi = matched.Count,
                SapFisNolari = matched.Select(x => x.FisNo!).ToList(),

                ZiyaretEslesmeBitisSiniri = matchEnd,

                EslesmeDurumu = Eslesmedi,

                // Fiziksel çıkış saati elimizde yok.
                IcSureDogrulandi = false,

                // Her SAP fişi bağımsız işlem olarak gösterilir.
                SapIslemleri = matched.Select(ToSapIslem).ToList()
            };

            CalculateDurations(dto, visit, sample, first, second, completedWeighings);

            ApplyMatchingConfidence(
                dto, visit, matched, conflictingCount,
                sample, first, second, nextEntry, matchEnd);

            ApplyStatus(
                dto, visit, matched, completedWeighings,
                sample, first, nextEntry, maxVisitEnd, now);

            if (dto.EslesmeDurumu == KontrolGerekli)
            {
                dto.Uyarilar.Add(
                    "Bu ziyaretin KPI sonuçları " +
                    "kontrol edilmeden kesin " +
                    "performans verisi sayılmamalıdır.");
            }

            return dto;
        }

        private static DateTime? FindNextEntry(
            Ziyaret visit,
            Dictionary<string, List<Ziyaret>> entryGroups)
        {
            if (!entryGroups.TryGetValue(visit.NormalizedPlaka, out var entries))
                return null;

            return entries
                .FirstOrDefault(x =>
                    x.Giris > visit.Giris ||
                    (x.Giris == visit.Giris && x.Id > visit.Id))
                ?.Giris;
        }

        private static KantarSapIslemDto ToSapIslem(SapKantarDto x)
        {
            return new KantarSapIslemDto
            {
                FisNo = x.FisNo,
                KantarId = x.KantarId,
                Plaka = x.Plaka,

                IlkTartimZamani = x.FirstAt,
                IkinciTartimZamani = x.SecondAt,

                IlkAgirlik = x.FirstValue,
                IkinciAgirlik = x.SecondValue,
                NetAgirlik = x.NetValue,

                Tamamlandi = x.Finished,

                IslemSuresiDakika = IsCompleted(x)
                    ? Math.Round((x.SecondAt!.Value - x.FirstAt!.Value).TotalMinutes, 2)
                    : null,

                Durum = x.Finished && x.SecondAt.HasValue
                    ? Tamamlandi
                    : "DEVAM_EDIYOR"
            };
        }

        private static void CalculateDurations(
            KantarZiyaretKpiDto dto,
            Ziyaret visit,
            DateTime? sample,
            DateTime? first,
            DateTime? second,
            List<SapKantarDto> completedWeighings)
        {
            // Güvenlik -> Numune
            dto.GiristenNumuneyeDakika = Minutes(visit.Giris, sample);

            // Numune -> İlk SAP tartımı
            dto.NumunedenIlkTartimaDakika = Minutes(sample, first);

            // Güvenlik -> İlk SAP tartımı
            dto.GiristenIlkTartimaDakika = Minutes(visit.Giris, first);

            // Her SAP fişinin süresini ayrı hesapla.
            // Birden fazla fişin ilk/son saatlerini
            // tek işlemmiş gibi kullanma.
            double? totalWeighingMinutes =
                completedWeighings.Count > 0
                    ? Math.Round(
                        completedWeighings.Sum(x =>
                            (x.SecondAt!.Value - x.FirstAt!.Value).TotalMinutes),
                        2)
                    : null;

            dto.SapTartimToplamDakika = totalWeighingMinutes;

            // Mevcut mobil uygulamanın okuduğu alan.
            dto.IlkTartimdanSonTartimaDakika = totalWeighingMinutes;

            // Fişler arasında boşluk varsa ayrıca ölç.
            double totalGapMinutes = 0;

            for (int i = 1; i < completedWeighings.Count; i++)
            {
                var previousEnd = completedWeighings[i - 1].SecondAt!.Value;
                var currentStart = completedWeighings[i].FirstAt!.Value;

                if (currentStart > previousEnd)
                {
                    totalGapMinutes += (currentStart - previousEnd).TotalMinutes;
                }
            }

            dto.SapFislerArasiBoslukDakika =
                completedWeighings.Count > 1
                    ? Math.Round(totalGapMinutes, 2)
                    : null;

            // Güvenlik girişinden son tamamlanan
            // ikinci tartıma kadar geçen süre.
            // Bu fiziksel çıkış süresi değildir.
            dto.ToplamSureDakika = Minutes(visit.Giris, second);
        }

        private static void ApplyMatchingConfidence(
            KantarZiyaretKpiDto dto,
            Ziyaret visit,
            List<SapKantarDto> matched,
            int conflictingCount,
            DateTime? sample,
            DateTime? first,
            DateTime? second,
            DateTime? nextEntry,
            DateTime matchEnd)
        {
            if (matched.Count == 0)
            {
                dto.EslesmeDurumu = Eslesmedi;
            }
            else if (conflictingCount > 0)
            {
                dto.EslesmeDurumu = KontrolGerekli;

                AddInconsistency(dto,
                    "Bazı SAP fişleri ziyaret " +
                    "zaman sınırını aşıyor.");
            }
            else if (matched.Count > 1)
            {
                dto.EslesmeDurumu = CokluFisKontrol;

                dto.Uyarilar.Add(
                    "Birden fazla SAP fişi aynı " +
                    "güvenlik ziyaretine eşleşti. " +
                    "Aynı ziyaret olduğu doğrulanmalı.");
            }
            else
            {
                dto.EslesmeDurumu = ZamanEslesti;
            }

            if (conflictingCount > 0 && matched.Count == 0)
            {
                AddInconsistency(dto,
                    "Ziyaret sınırını aşan SAP " +
                    "fişleri otomatik eşleştirilmedi.");
            }

            if (sample.HasValue && sample.Value < visit.Giris)
            {
                AddInconsistency(dto,
                    "Numune zamanı güvenlik " +
                    "girişinden önce.");
            }

            if (sample.HasValue && sample.Value >= matchEnd)
            {
                AddInconsistency(dto,
                    "Numune zamanı bu ziyaretin " +
                    "eşleştirme sınırını aşıyor.");
            }

            if (sample.HasValue && first.HasValue && first.Value < sample.Value)
            {
                AddInconsistency(dto,
                    "İlk SAP tartımı numune " +
                    "kaydından önce.");
            }

            if (!sample.HasValue && first.HasValue)
            {
                AddInconsistency(dto,
                    "Numune kaydı yok ancak " +
                    "SAP tartımı mevcut.");
            }

            if (matched.Any(x => x.SecondAt.HasValue && !x.Finished))
            {
                dto.Uyarilar.Add(
                    "İkinci tartım zamanı var " +
                    "ancak SAP fişi tamamlanmamış.");
            }

            if (matched.Any(x =>
                x.FirstAt.HasValue &&
                x.SecondAt.HasValue &&
                x.SecondAt.Value < x.FirstAt.Value))
            {
                AddInconsistency(dto,
                    "SAP ikinci tartımı ilk " +
                    "tartımdan önce görünüyor.");
            }

            if (nextEntry.HasValue && second.HasValue && second.Value >= nextEntry.Value)
            {
                AddInconsistency(dto,
                    "Son tartım sonraki güvenlik " +
                    "girişinin zamanına taşıyor.");

                dto.ToplamSureDakika = null;
            }

            if (dto.VeriTutarsizligi)
            {
                dto.EslesmeDurumu = KontrolGerekli;
            }
        }

        private static void ApplyStatus(
            KantarZiyaretKpiDto dto,
            Ziyaret visit,
            List<SapKantarDto> matched,
            List<SapKantarDto> completedWeighings,
            DateTime? sample,
            DateTime? first,
            DateTime? nextEntry,
            DateTime maxVisitEnd,
            DateTime now)
        {
            var incomplete = matched
                .Where(x => !x.Finished || !x.SecondAt.HasValue)
                .ToList();

            // Devam eden son fişin başlangıcı.
            var lastIncompleteStart = incomplete.Max(x => x.FirstAt);

            if (!first.HasValue && !sample.HasValue)
            {
                dto.Durum = NumuneBekliyor;
                dto.MevcutAsamadaBeklemeDakika = Minutes(visit.Giris, now);
                dto.Aciklama =
                    $"{FormatMinutes(dto.MevcutAsamadaBeklemeDakika)} " +
                    "numune bekleniyor.";
            }
            else if (!first.HasValue)
            {
                dto.Durum = KantarBekliyor;
                dto.MevcutAsamadaBeklemeDakika = Minutes(sample, now);
                dto.Aciklama =
                    "Numune kaydı oluşturuldu. " +
                    $"{FormatMinutes(dto.MevcutAsamadaBeklemeDakika)} " +
                    "ilk tartım bekleniyor.";
            }
            else if (completedWeighings.Count == 0)
            {
                dto.Durum = TartimDevamEdiyor;
                dto.MevcutAsamadaBeklemeDakika = Minutes(lastIncompleteStart, now);
                dto.Aciklama =
                    "İlk tartım yapıldı. " +
                    $"{FormatMinutes(dto.MevcutAsamadaBeklemeDakika)} " +
                    "ikinci tartım bekleniyor.";
            }
            else if (incomplete.Count > 0)
            {
                dto.Durum = KismiTamamlandi;
                dto.MevcutAsamadaBeklemeDakika = Minutes(lastIncompleteStart, now);
                dto.Aciklama =
                    "Bazı SAP fişleri tamamlandı, " +
                    "bazıları devam ediyor.";
            }
            else
            {
                dto.Durum = Tamamlandi;
                dto.MevcutAsamadaBeklemeDakika = null;
                dto.Aciklama =
                    "Güvenlik girişinden son " +
                    "tamamlanan SAP tartımına kadar " +
                    $"{FormatMinutes(dto.ToplamSureDakika)} geçti.";
            }

            // Geçmişte kalan eksik ziyaretleri
            // bugün hâlâ bekliyormuş gibi gösterme.
            var historical = visit.Giris.Date < now.Date;

            // Aynı plakanın yeni girişi varsa
            // önceki ziyaret artık aktif sayılamaz.
            var superseded = nextEntry.HasValue && nextEntry.Value <= now;

            // 24 saatlik geçici sınır aşıldığında
            // canlı bekleme göstermek güvenilir değil.
            var expired = maxVisitEnd <= now;

            if (!(historical || superseded || expired))
                return;

            if (dto.Durum == KismiTamamlandi)
            {
                // Kısmen tamamlanan eski kayıtlar da
                // aktif tartım sayılmamalı.
                dto.MevcutAsamadaBeklemeDakika = null;
                dto.Aciklama =
                    "Kısmen tamamlanmış geçmiş SAP " +
                    "kayıtları bulunuyor. Güncel " +
                    "bekleme süresi bilinmiyor.";
            }
            else if (dto.Durum != Tamamlandi)
            {
                dto.Durum = GecmisEksikKayit;
                dto.MevcutAsamadaBeklemeDakika = null;
                dto.Aciklama =
                    "Bu ziyaretin tamamlandığı " +
                    "doğrulanamadı. Güncel bekleme " +
                    "süresi hesaplanmıyor.";
                dto.Uyarilar.Add(
                    "Gerçek fabrika çıkış zamanı " +
                    "bulunmadığı için geçmiş " +
                    "bekleme süresi belirsiz.");
            }
        }

        #endregion

        #region Rapor Özeti

        private static bool AktifTartimda(KantarZiyaretKpiDto x)
        {
            return x.Durum == TartimDevamEdiyor ||
                   (x.Durum == KismiTamamlandi && x.MevcutAsamadaBeklemeDakika.HasValue);
        }

        private static void FillGeneralKpi(
            KantarKpiRaporDto report,
            List<SapKantarDto> sapRows,
            HashSet<string> assigned)
        {
            report.ToplamZiyaret = report.Ziyaretler.Count;
            report.NumuneBekleyen = report.Ziyaretler.Count(x => x.Durum == NumuneBekliyor);
            report.KantarBekleyen = report.Ziyaretler.Count(x => x.Durum == KantarBekliyor);
            report.Tartimda = report.Ziyaretler.Count(AktifTartimda);
            report.Tamamlanan = report.Ziyaretler.Count(x => x.Durum == Tamamlandi);

            report.EslesmeyenSapFisSayisi =
                sapRows.Count(x => !assigned.Contains(SapKey(x)));
        }

        private static List<KantarFirmaKpiDto> BuildFirmaKpis(
            List<KantarZiyaretKpiDto> ziyaretler)
        {
            static bool Guvenilir(KantarZiyaretKpiDto x) =>
                x.EslesmeDurumu == ZamanEslesti && !x.VeriTutarsizligi;

            return ziyaretler
                .GroupBy(x => new { x.FirmaId, x.FirmaAdi })
                .Select(g => new KantarFirmaKpiDto
                {
                    FirmaId = g.Key.FirmaId,
                    FirmaAdi = g.Key.FirmaAdi,

                    ZiyaretSayisi = g.Count(),
                    NumuneBekleyenSayisi = g.Count(x => x.Durum == NumuneBekliyor),
                    KantarBekleyenSayisi = g.Count(x => x.Durum == KantarBekliyor),
                    TartimdaSayisi = g.Count(AktifTartimda),
                    TamamlananSayisi = g.Count(x => x.Durum == Tamamlandi),

                    OrtalamaNumuneTepkiDakika = Avg(g.Select(x => x.GiristenNumuneyeDakika)),
                    OrtalamaKantaraGirisDakika = Avg(g.Select(x => x.GiristenIlkTartimaDakika)),
                    OrtalamaTartimlarArasiDakika = Avg(g.Select(x => x.SapTartimToplamDakika)),

                    OrtalamaToplamSureDakika = Avg(g.Select(x =>
                        Guvenilir(x) ? x.ToplamSureDakika : null)),

                    ToplamGerceklesenSureDakika = Math.Round(
                        g.Where(Guvenilir).Sum(x => x.ToplamSureDakika ?? 0),
                        2)
                })
                .OrderBy(x => x.FirmaAdi)
                .ToList();
        }

        private static void AddGeneralWarnings(
            KantarKpiRaporDto report,
            bool kantarFiltreli)
        {
            report.Uyarilar.Add(
                "Numune reaksiyon süresi fiziksel " +
                "numune zamanına değil, SQL kayıt " +
                "zamanına dayanır.");

            report.Uyarilar.Add(
                "SAP ikinci tartımı gerçek fabrika " +
                "çıkışı anlamına gelmez.");

            report.Uyarilar.Add(
                "Tartım süresi, her SAP fişinin " +
                "ilk ve ikinci tartımı arasındaki " +
                "sürelerin toplamıdır.");

            report.Uyarilar.Add(
                "Birden fazla SAP fişi bulunan " +
                "ziyaretler ayrıca kontrol edilmelidir.");

            report.Uyarilar.Add(
                "Fiziksel çıkış kaydı olmadığından " +
                "ziyaret süresi kesin olarak " +
                "doğrulanamaz.");

            report.Uyarilar.Add(
                "24 saatlik ziyaret eşleştirme " +
                "sınırı geçici bir operasyonel " +
                "varsayımdır.");

            report.Uyarilar.Add(
                "SAP tarih sorgusunun kayıt sınırı " +
                "doğrulanmalıdır. Eksik SAP " +
                "kayıtları KPI hesaplarını etkiler.");

            report.Uyarilar.Add(
                "Üretim/boşaltma hızını hesaplamak " +
                "için ayrıca işlem başlangıcı, " +
                "bitişi ve miktar bilgisi gerekir.");

            if (kantarFiltreli)
            {
                report.Uyarilar.Add(
                    "Kantar filtresi SAP fişlerine " +
                    "uygulanır; güvenlik ziyaretleri " +
                    "ayrıca tesis bazında filtrelenmez.");
            }
        }

        #endregion

        #region Helpers

        private static string SapKey(SapKantarDto x) => $"{x.KantarId}:{x.FisNo}";

        private static bool IsCompleted(SapKantarDto x)
        {
            return x.Finished &&
                   x.FirstAt.HasValue &&
                   x.SecondAt.HasValue &&
                   x.SecondAt.Value >= x.FirstAt.Value;
        }

        // Veri tutarsızlığı uyarısı: ziyaret KONTROL_GEREKLI sayılır.
        private static void AddInconsistency(KantarZiyaretKpiDto dto, string message)
        {
            dto.Uyarilar.Add(message);
            dto.VeriTutarsizligi = true;
        }

        private static double? Minutes(DateTime? from, DateTime? to)
        {
            if (!from.HasValue || !to.HasValue || to.Value < from.Value)
                return null;

            return Math.Round((to.Value - from.Value).TotalMinutes, 2);
        }

        private static double? Avg(IEnumerable<double?> values)
        {
            var valid = values
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToArray();

            return valid.Length == 0
                ? null
                : Math.Round(valid.Average(), 2);
        }

        private static string FormatMinutes(double? minutes)
        {
            if (!minutes.HasValue)
                return "Hesaplanamadı";

            var totalSeconds = (long)Math.Round(
                minutes.Value * 60,
                MidpointRounding.AwayFromZero);

            var duration = TimeSpan.FromSeconds(totalSeconds);

            if (duration.TotalHours >= 1)
            {
                return
                    $"{(int)duration.TotalHours} saat " +
                    $"{duration.Minutes} dakika " +
                    $"{duration.Seconds} saniye";
            }

            if (duration.TotalMinutes >= 1)
            {
                return
                    $"{duration.Minutes} dakika " +
                    $"{duration.Seconds} saniye";
            }

            return $"{duration.Seconds} saniye";
        }

        #endregion
    }
}
