using KumesScada.Application.Interfaces;
using KumesScada.Application.RequestModels;
using KumesScada.Application.ViewModels;
using KumesScada.Domain.Models;
using Microsoft.EntityFrameworkCore;

// ENTITY NAMESPACE'İNİ SENDEKİNE GÖRE EKLE
// using KumesScada.Domain.Entities;

namespace KumesScada.Infrastructure.Services.VG
{
    public sealed class VGYumurtlamaQueryService
        : IVGYumurtlamaQueryService
    {
        private readonly KumesScadaDbContext _db;
        private readonly IVGYumurtlamaService _calculator;
        private readonly IVgYumurtlamaDonemAppService _donemService;

        public VGYumurtlamaQueryService(
            KumesScadaDbContext db,
            IVGYumurtlamaService calculator,
            IVgYumurtlamaDonemAppService donemService)
        {
            _db = db;
            _calculator = calculator;
            _donemService = donemService;
        }

        public async Task<VGYumurtlamaViewModel> GetAsync(
            VGYumurtlamaQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.KumesNo))
                throw new ArgumentException("Kümes numarası zorunludur.");

            // =====================================================
            // KÜMES BUL
            // =====================================================

            var kumes = await _db.Set<Kumesler>()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.KumesNo != null &&
                        x.KumesNo == request.KumesNo,
                    cancellationToken);

            if (kumes == null)
                throw new Exception(
                    $"{request.KumesNo} numaralı kümes bulunamadı.");

            var kumesId = kumes.Id;

            // "Year" belirtilmezse kümesin TÜM tarihçesi getirilir — bir
            // parti birden fazla takvim yılına yayılabilir (bkz.
            // VGYumurtlamaQueryRequest.Year üzerindeki not). AMA alt sınır
            // hiçbir zaman BU DÖNEMİN kümese giriş tarihinden öncesine
            // gidemez — aksi halde aynı KumesNo altında daha önce
            // kaydedilmiş ÖNCEKİ bir partinin (farklı hayvan grubu,
            // farklı BaslangicHayvanSayisi) günlük verileri bu partiyle
            // karışır: GÜN negatif çıkar, TAVUK SAYISI'nın kümülatif
            // ölüm zinciri yanlış günden başlar, HAFTA hesabı da buna
            // bağlı olarak kayar (bkz. proje notları — T14 135.PARTİ'de
            // Kümes 14'ün 2026-07-27 öncesi, önceki partiye ait verisiyle
            // doğrulandı).
            // İstek query string'den geldiği için (ör. "...T21:00:00.000Z")
            // ASP.NET model binder bunu Kind=Utc olarak ayrıştırıyor —
            // ama veritabanındaki YumurtlamaDonem.GirisTarihi YEREL
            // takvim tarihi olarak saklanıyor (ör. 07.03.2025, UTC
            // karşılığı 06.03.2025 21:00). .Date ile doğrudan kesersek
            // bir gün GERİYE kayar ve aşağıdaki "sonraki dönem" sorgusu
            // dönemin KENDİ GirisTarihi'ni "sonraki parti" sanıp
            // endDate'i birkaç saatlik bir pencereye sıkıştırır (bkz.
            // GetNextGirisTarihiByKumesNo) — bu yüzden önce yerel saate
            // çevriliyor.
            var donemBaslangici = request.KumeseGirisTarihi.Kind == DateTimeKind.Utc
                ? request.KumeseGirisTarihi.ToLocalTime()
                : request.KumeseGirisTarihi;

            var yilBaslangici = request.Year.HasValue
                ? new DateTime(request.Year.Value, 1, 1)
                : (DateTime?)null;

            var startDate = yilBaslangici.HasValue && yilBaslangici.Value > donemBaslangici
                ? yilBaslangici.Value
                : donemBaslangici;

            var endDate = request.Year.HasValue
                ? yilBaslangici!.Value.AddYears(1)
                : DateTime.MaxValue.Date;

            // Üst sınır, alt sınırla (donemBaslangici) SİMETRİK bir
            // koruma: bu dönemden SONRA kümese yeni bir parti girmişse
            // (bkz. YumurtlamaDonem — aynı KumesNo'da birden fazla dönem
            // satırı olabilir), bu dönemin hesaplaması o sonraki partinin
            // günlük verisine taşmasın — aksi halde "en güncel değil,
            // eski bir dönem" için kart/sorgu istenirse GÜN/HAFTA hesabı
            // bir sonraki partinin verisini de işin içine katardı. "En
            // güncel" dönem için zaten sonraki parti olmadığından bu
            // sorgu null döner ve davranış değişmez.
            var sonrakiDonemBaslangici = await _donemService.GetNextGirisTarihiByKumesNo(
                request.KumesNo, donemBaslangici, cancellationToken);

            if (sonrakiDonemBaslangici.HasValue && sonrakiDonemBaslangici.Value < endDate)
                endDate = sonrakiDonemBaslangici.Value;


            // =====================================================
            // ÖLÜM
            // =====================================================

            var olumlar = await _db.Set<Olum>()
                .AsNoTracking()
                .Where(x =>
                    x.KumeslerId == kumesId &&
                    x.OlumTarih >= startDate &&
                    x.OlumTarih < endDate)
                .ToListAsync(cancellationToken);


            // =====================================================
            // YUMURTA AĞIRLIK
            // =====================================================

            var yumurtaAgirliklar =
                await _db.Set<YumurtaAgirlik>()
                    .AsNoTracking()
                    .Where(x =>
                        x.KumeslerId == kumesId &&
                        x.YumurtaAgirlikTarih >= startDate &&
                        x.YumurtaAgirlikTarih < endDate)
                    .ToListAsync(cancellationToken);


            // =====================================================
            // YEM
            // =====================================================

            var yemler = await _db.Set<Yem>()
                .AsNoTracking()
                .Where(x =>
                    x.KumeslerId == kumesId &&
                    x.YemTarih >= startDate &&
                    x.YemTarih < endDate)
                .ToListAsync(cancellationToken);


            // =====================================================
            // SU
            // =====================================================

            var sular = await _db.Set<Su>()
                .AsNoTracking()
                .Where(x =>
                    x.KumeslerId == kumesId &&
                    x.SuTarih >= startDate &&
                    x.SuTarih < endDate)
                .ToListAsync(cancellationToken);


            // =====================================================
            // UYGULAMA
            // =====================================================

            var uygulamalar = await _db.Set<Uygulama>()
                .AsNoTracking()
                .Where(x =>
                    x.KumeslerId == kumesId &&
                    x.UygulamalarTarih >= startDate &&
                    x.UygulamalarTarih < endDate)
                .ToListAsync(cancellationToken);


            // =====================================================
            // CANLI AĞIRLIK
            // =====================================================

            var canliAgirliklar = await _db.Set<CanliAgirlik>()
                .AsNoTracking()
                .Where(x =>
                    x.KumeslerId == kumesId &&
                    x.CanliAgirlikTarih >= startDate &&
                    x.CanliAgirlikTarih < endDate)
                .ToListAsync(cancellationToken);


            // =====================================================
            // TEK SIRA RANDIMAN
            // =====================================================

            var randimanlar = await _db.Set<Randiman>()
                .AsNoTracking()
                .Where(x =>
                    x.KumeslerId == kumesId &&
                    x.RandimanTarih >= startDate &&
                    x.RandimanTarih < endDate)
                .ToListAsync(cancellationToken);


            // =====================================================
            // VBAP RANDIMAN
            // =====================================================

            var vbapRandimanlar = await _db.Set<VbapRandiman>()
                .AsNoTracking()
                .Where(x =>
                    x.KumeslerId == kumesId &&
                    x.VbapRandimanTarih >= startDate &&
                    x.VbapRandimanTarih < endDate)
                .ToListAsync(cancellationToken);


            // =====================================================
            // KÜMES VERİ
            // =====================================================

            var kumesVerileri = await _db.Set<KumesVeri>()
                .AsNoTracking()
                .Where(x =>
                    x.KumeslerId == kumesId &&
                    x.Tarih >= startDate &&
                    x.Tarih < endDate)
                .ToListAsync(cancellationToken);


            // =====================================================
            // LOOKUP TABLOLARI
            // =====================================================

            var yumurtaDurumlari =
                await _db.Set<KumesYumurtaDurum>()
                    .AsNoTracking()
                    .ToDictionaryAsync(
                        x => x.KumesYumurtaDurumId,
                        x => x.KumesYumurtaDurumAd,
                        cancellationToken);


            var kaliteDurumlari =
                await _db.Set<KumesYumurtaKaliteDurum>()
                    .AsNoTracking()
                    .ToDictionaryAsync(
                        x => x.KumesYumurtaKaliteDurumId,
                        x => x.KumesYumurtaKaliteDurumAd,
                        cancellationToken);


            var hayvanDetaylari =
                await _db.Set<KumesHayvanDetay>()
                    .AsNoTracking()
                    .ToDictionaryAsync(
                        x => x.KumesHayvanDetayId,
                        x => x.KumesHayvanDetayAdi,
                        cancellationToken);


            // =====================================================
            // TARİHLER
            // =====================================================

            var dates = olumlar
                .Select(x => x.OlumTarih.Date)

                .Concat(
                    yumurtaAgirliklar
                        .Select(x =>
                            x.YumurtaAgirlikTarih.Date))

                .Concat(
                    yemler
                        .Select(x =>
                            x.YemTarih.Date))

                .Concat(
                    sular
                        .Select(x =>
                            x.SuTarih.Date))

                .Concat(
                    uygulamalar
                        .Select(x =>
                            x.UygulamalarTarih.Date))

                .Concat(
                    kumesVerileri
                        .Select(x =>
                            x.Tarih.Date))

                .Concat(
                    canliAgirliklar
                        .Select(x =>
                            x.CanliAgirlikTarih.Date))

                .Concat(
                    randimanlar
                        .Select(x =>
                            x.RandimanTarih.Date))

                .Concat(
                    vbapRandimanlar
                        .Select(x =>
                            x.VbapRandimanTarih.Date))

                .Distinct()

                .OrderBy(x => x)

                .ToList();


            // =====================================================
            // HESAPLAMA REQUEST
            // =====================================================

            var calculateRequest =
                new VGYumurtlamaRequest
                {
                    KumesNo =
                        request.KumesNo,

                    Irk =
                        request.Irk,

                    KumeseGirisTarihi =
                        request.KumeseGirisTarihi,

                    BaslangicHayvanSayisi =
                        request.BaslangicHayvanSayisi,

                    IcSiparisNo =
                        request.IcSiparisNo,

                    GirisYasiGun =
                        request.GirisYasiGun,

                    GirisHaftasi =
                        request.GirisHaftasi
                };


            foreach (var date in dates)
            {
                var row =
                    new VGYumurtlamaKaynakSatirRequest
                    {
                        Tarih = date
                    };


                // =================================================
                // ÖLÜM
                // =================================================

                row.GunlukOlum =
                    olumlar
                        .Where(x =>
                            x.OlumTarih.Date == date)
                        .Select(x =>
                            (decimal?)x.OlumMiktar)
                        .FirstOrDefault();


                // =================================================
                // YUMURTA GRAMAJI
                // =================================================

                row.YumurtaGramaji =
                    yumurtaAgirliklar
                        .Where(x =>
                            x.YumurtaAgirlikTarih.Date == date)
                        .Select(x =>
                            (decimal?)x.YumurtaAgirlikMiktar)
                        .FirstOrDefault();


                // =================================================
                // YEM
                // =================================================

                row.YemKg =
                    yemler
                        .Where(x =>
                            x.YemTarih.Date == date)
                        .Select(x =>
                            (decimal?)x.YemMiktar)
                        .FirstOrDefault();


                // =================================================
                // SU
                // =================================================

                row.SuLitre =
                    sular
                        .Where(x =>
                            x.SuTarih.Date == date)
                        .Select(x =>
                            (decimal?)x.SuMiktar)
                        .FirstOrDefault();


                // =================================================
                // UYGULAMA
                // =================================================

                // "Uygulama" tablosu Uygulamalar dışında Kümes Personeli/
                // Yemin Rasyon No'yu da Tip ayırt edici sütunuyla
                // paylaşıyor (bkz. Uygulama.Tip) — eski (Tip NULL)
                // kayıtlar "Uygulama" ile eşdeğer sayılır.
                row.Uygulama =
                    uygulamalar
                        .Where(x =>
                            x.UygulamalarTarih.Date == date &&
                            (x.Tip == "Uygulama" || x.Tip == null))
                        .Select(x =>
                            x.UygulamaAciklama)
                        .FirstOrDefault();

                row.KumesPersoneli =
                    uygulamalar
                        .Where(x =>
                            x.UygulamalarTarih.Date == date &&
                            x.Tip == "KumesPersoneli")
                        .Select(x =>
                            x.UygulamaAciklama)
                        .FirstOrDefault();

                row.YeminRasyonNo =
                    uygulamalar
                        .Where(x =>
                            x.UygulamalarTarih.Date == date &&
                            x.Tip == "YeminRasyonNo")
                        .Select(x =>
                            x.UygulamaAciklama)
                        .FirstOrDefault();


                // =================================================
                // CANLI AĞIRLIK
                // =================================================

                row.CanliAgirlik =
                    canliAgirliklar
                        .Where(x =>
                            x.CanliAgirlikTarih.Date == date)
                        .Select(x =>
                            (decimal?)x.CanliAgirlikMiktar)
                        .FirstOrDefault();


                // =================================================
                // TEK SIRA / VBAP RANDIMAN
                // =================================================

                row.TekSiraRandiman =
                    randimanlar
                        .Where(x =>
                            x.RandimanTarih.Date == date)
                        .Select(x =>
                            (decimal?)x.RandimanMiktar)
                        .FirstOrDefault();

                row.VbapRandiman =
                    vbapRandimanlar
                        .Where(x =>
                            x.VbapRandimanTarih.Date == date)
                        .Select(x =>
                            (decimal?)x.VbapRandimanMiktar)
                        .FirstOrDefault();


                // =================================================
                // KÜMES VERİLERİ
                // =================================================

                var dailyKumesData =
                    kumesVerileri
                        .Where(x =>
                            x.Tarih.Date == date)
                        .ToList();


                foreach (var item in dailyKumesData)
                {
                    // =============================================
                    // YUMURTA DURUM
                    // =============================================

                    if (item.KumesYumurtaDurumId.HasValue &&
                        yumurtaDurumlari.TryGetValue(
                            item.KumesYumurtaDurumId.Value,
                            out var yumurtaDurumAdi))
                    {
                        var name =
                            Normalize(yumurtaDurumAdi);

                        switch (name)
                        {
                            case "SAGLAM":
                            case "SAGLAMYUMURTA":

                                row.SaglamYumurta =
                                    item.KumesYumurtaDurumDeger;

                                break;


                            case "KIRIK":
                            case "KIRIKYUMURTA":

                                row.KirikYumurta =
                                    item.KumesYumurtaDurumDeger;

                                break;


                            case "KIRLI":
                            case "KIRLIYUMURTA":

                                row.KirliYumurta =
                                    item.KumesYumurtaDurumDeger;

                                break;


                            case "KUCUKKIRIK":

                                row.KucukKirik =
                                    item.KumesYumurtaDurumDeger;

                                break;


                            case "FIRE":

                                row.Fire =
                                    item.KumesYumurtaDurumDeger;

                                break;


                            case "LIKIDKABUKSUZZAR":

                                row.LikidKabuksuzZar =
                                    item.KumesYumurtaDurumDeger;

                                break;


                            case "ZARLIYUMURTA(KG)":

                                row.ZarliYumurtaKg =
                                    item.KumesYumurtaDurumDeger;

                                break;


                            case "MOBAKIRIKDEGERI":

                                row.MobaKirikDegeri =
                                    item.KumesYumurtaDurumDeger;

                                break;


                            case "MOBAKIRLIDEGERI(0,1,2,3)":

                                row.MobaKirliDegeri =
                                    item.KumesYumurtaDurumDeger;

                                break;


                            case "HAMYUMURTAADEDI":

                                row.ToplamYumurtaAdedi =
                                    item.KumesYumurtaDurumDeger;

                                break;


                            case "KLAVUZ":

                                row.Klavuz =
                                    item.KumesYumurtaDurumDeger;

                                break;


                            case "SMALL":

                                row.Small =
                                    item.KumesYumurtaDurumDeger;

                                break;


                            case "MEDIUM":

                                row.Medium =
                                    item.KumesYumurtaDurumDeger;

                                break;


                            case "LARGE":

                                row.Large =
                                    item.KumesYumurtaDurumDeger;

                                break;


                            case "JUMBO":

                                row.Jumbo =
                                    item.KumesYumurtaDurumDeger;

                                break;


                            case "S.JUMBO":
                            case "SJUMBO":

                                row.SJumbo =
                                    item.KumesYumurtaDurumDeger;

                                break;


                            case "CIFTSARI":

                                row.CiftSari =
                                    item.KumesYumurtaDurumDeger;

                                break;
                        }
                    }


                    // =============================================
                    // HAYVAN DETAY
                    // =============================================

                    if (item.KumesHayvanDetayId.HasValue &&
                        hayvanDetaylari.TryGetValue(
                            item.KumesHayvanDetayId.Value,
                            out var hayvanDetayAdi))
                    {
                        var name =
                            Normalize(hayvanDetayAdi);

                        switch (name)
                        {
                            case "HAYVANYASI(HF)":

                                row.HayvanYasiHafta =
                                    item.KumesHayvanDetayDeger;

                                break;


                            case "KUMESCEKIMSURE(DK)":

                                row.CekimSuresiDk =
                                    item.KumesHayvanDetayDeger;

                                break;


                            case "SAATTECEKILENYUM.ADET":

                                row.SaatteCekilenYumurtaAdet =
                                    item.KumesHayvanDetayDeger;

                                break;


                            case "FIILIHAYVANSAYISI":

                                row.FiiliHayvanSayisi =
                                    item.KumesHayvanDetayDeger;

                                break;


                            case "FIILIYUMURTLAYANHAYVANSAYISI":

                                row.FiiliYumurtlayanHayvanSayisi =
                                    item.KumesHayvanDetayDeger;

                                break;
                        }
                    }


                    // =============================================
                    // KALİTE DURUM
                    // =============================================

                    if (item.KumesYumurtaKaliteDurumId.HasValue &&
                        kaliteDurumlari.TryGetValue(
                            item.KumesYumurtaKaliteDurumId.Value,
                            out var kaliteAdi))
                    {
                        var name =
                            Normalize(kaliteAdi);

                        switch (name)
                        {
                            case "GIZLICATLAK":

                                row.GizliCatlak =
                                    item.KumesYumurtaKaliteDurumDeger;

                                break;


                            case "KABUKDIRENCI":
                            case "MUKAVEMET":

                                row.KabukDirenci =
                                    item.KumesYumurtaKaliteDurumDeger;

                                break;


                            case "HU":
                            case "HAUGHUNIT":

                                row.Hu =
                                    item.KumesYumurtaKaliteDurumDeger;

                                break;
                        }
                    }
                }


                calculateRequest.Veriler.Add(row);
            }


            return _calculator.Calculate(
                calculateRequest);
        }


        public async Task<List<VGYumurtlamaKumesSecenegiViewModel>> GetKumeslerAsync(
            int lokasyonId,
            CancellationToken cancellationToken = default)
        {
            return await _db.Set<Kumesler>()
                .AsNoTracking()
                .Where(x =>
                    x.LokasyonId == lokasyonId &&
                    x.Silindi != true)
                .OrderBy(x => x.KumesName)
                .Select(x => new VGYumurtlamaKumesSecenegiViewModel
                {
                    Id = x.Id,
                    KumesId = x.KumesId,
                    KumesName = x.KumesName,
                    KumesNo = x.KumesNo,
                    Aktif = x.Aktif
                })
                .ToListAsync(cancellationToken);
        }


        private static string Normalize(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Trim()
                .ToUpperInvariant()

                // DİKKAT: ToUpperInvariant() Türkçe noktasız "ı" harfini
                // BÜYÜTMÜYOR (lowercase kalıyor, U+0131) — bu yüzden ayrıca
                // elle "I"ya çevrilmesi gerekiyor, aksi halde "Kırık"/
                // "Küçük Kırık" gibi adlar "KIRIK"/"KUCUKKIRIK" ile hiç
                // eşleşmez (bkz. canlı ortamda doğrulanan karakter kodu
                // teşhisi: normalize sonucu "KıRıK" = kod 75,305,82,305,75).
                .Replace("ı", "I")
                .Replace("İ", "I")
                .Replace("Ş", "S")
                .Replace("Ğ", "G")
                .Replace("Ü", "U")
                .Replace("Ö", "O")
                .Replace("Ç", "C")

                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "");
        }
    }
}