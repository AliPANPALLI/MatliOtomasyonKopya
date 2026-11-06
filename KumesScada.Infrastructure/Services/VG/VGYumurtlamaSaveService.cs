using KumesScada.Application.Interfaces;
using KumesScada.Application.ViewModels;
using KumesScada.Domain.Models;
using KumesScada.Domain.Models.Excel;
using Microsoft.EntityFrameworkCore;

namespace KumesScada.Infrastructure.Services.VG
{
    /// <summary>
    /// Excel'den okunan günlük kümes verilerini KumesScada veritabanına
    /// yazar. Yazdığı tablolar, <see cref="VGYumurtlamaQueryService"/>'in
    /// okuduğu TABLOLARLA BİREBİR AYNIDIR — böylece bir kez kaydedilen
    /// veri, sonradan veritabanından (excel'e tekrar gerek kalmadan)
    /// okunup hesaplanabilir.
    /// </summary>
    public sealed class VGYumurtlamaSaveService
        : IVGYumurtlamaSaveService
    {
        // "Uygulama" tablosu Uygulamalar dışında Kümes Personeli/Yemin
        // Rasyon No gibi serbest metin alanlarını da Tip ayırt edici
        // sütunuyla paylaşıyor (bkz. Uygulama.Tip, VGYumurtlamaQueryService).
        // Eski kayıtlarda Tip NULL'dır ve "Uygulama" ile eşdeğer sayılır.
        private const string UygulamaTipiUygulama = "Uygulama";
        private const string UygulamaTipiKumesPersoneli = "KumesPersoneli";
        private const string UygulamaTipiYeminRasyonNo = "YeminRasyonNo";

        private readonly KumesScadaDbContext _db;

        private readonly IVGYumurtlamaExcelSourceService _sourceService;

        public VGYumurtlamaSaveService(
            KumesScadaDbContext db,
            IVGYumurtlamaExcelSourceService sourceService)
        {
            _db = db;
            _sourceService = sourceService;
        }

        public async Task<VGYumurtlamaSaveResultViewModel> SaveAsync(
            Stream yemHesaplariStream,
            string yemHesaplariFileName,
            Stream? tasnifStream,
            string? tasnifFileName,
            int? year,
            string? kumesNo,
            CancellationToken cancellationToken = default)
        {
            var source = await _sourceService.ReadAsync(
                yemHesaplariStream,
                yemHesaplariFileName,
                tasnifStream,
                tasnifFileName,
                year,
                cancellationToken);

            return await SaveDataAsync(
                source.Data,
                source.Warnings,
                year,
                kumesNo,
                cancellationToken);
        }

        /// <summary>
        /// SaveAsync (excel stream'den okuyup kaydeden) ile ana excel'den
        /// geçmiş veri aktarımının (bkz. VGYumurtlamaKartExcelReader)
        /// PAYLAŞTIĞI upsert çekirdeği — kaynak ne olursa olsun
        /// (Günlük Yem Hesapları/Tasnif ya da ana excel'in "Yumurtlama "
        /// sayfası) aynı tablolara, aynı upsert mantığıyla yazar.
        /// </summary>
        public async Task<VGYumurtlamaSaveResultViewModel> SaveDataAsync(
            List<VGExcelKumesGunlukData> data,
            List<string> initialWarnings,
            int? year,
            string? kumesNo,
            CancellationToken cancellationToken = default)
        {
            var result = new VGYumurtlamaSaveResultViewModel
            {
                Warnings = initialWarnings
            };

            // "year" belirtilmezse dosyadaki TÜM günler kaydedilir —
            // tarihler excel'den zaten tam (gün/ay/yıl) okunuyor,
            // yıl burada sadece OPSİYONEL bir filtre.
            var rows = data
                .Where(x =>
                    (!year.HasValue || x.Tarih.Year == year.Value) &&
                    !string.IsNullOrWhiteSpace(x.KumesNo) &&
                    (string.IsNullOrWhiteSpace(kumesNo) ||
                     string.Equals(x.KumesNo, kumesNo, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (rows.Count == 0)
            {
                result.Warnings.Add(
                    "Kaydedilecek satır bulunamadı (dosya/yıl/kümes no eşleşmedi).");

                return result;
            }

            result.Years = rows
                .Select(x => x.Tarih.Year)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            // =====================================================
            // KÜMES NO -> ID EŞLEMESİ
            // =====================================================

            var kumeslerListesi = await _db.Set<Kumesler>()
                .Where(x => x.KumesNo != null)
                .ToListAsync(cancellationToken);

            // Aynı KumesNo'ya sahip birden fazla kayıt varsa (silinmiş/
            // eski kayıtlar vb.) ilkini kullan — ToDictionary'nin
            // duplicate key hatası vermesini engeller.
            var kumeslerByNo = kumeslerListesi
                .GroupBy(x => x.KumesNo!.Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.First().Id);

            var bulunamayanKumesler = rows
                .Select(x => x.KumesNo.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(x => !kumeslerByNo.ContainsKey(x.ToUpperInvariant()))
                .ToList();

            result.BulunamayanKumesler = bulunamayanKumesler;

            if (bulunamayanKumesler.Count > 0)
            {
                result.Warnings.Add(
                    "Şu kümes kodları 'Kumesler' tablosunda bulunamadı, " +
                    "bu kümeslerin verileri KAYDEDILMEDI: " +
                    string.Join(", ", bulunamayanKumesler) +
                    ". Önce Kümes Tanımlama ekranından kayıt açılmalı.");
            }

            rows = rows
                .Where(x => kumeslerByNo.ContainsKey(x.KumesNo.Trim().ToUpperInvariant()))
                .ToList();

            result.KumesCount = rows
                .Select(x => x.KumesNo.Trim().ToUpperInvariant())
                .Distinct()
                .Count();

            result.GunSatiriSayisi = rows.Count;


            // =====================================================
            // LOOKUP (DURUM / KALİTE DURUM) ID'LERİ
            // =====================================================

            var durumIdByName = (await _db.Set<KumesYumurtaDurum>()
                    .ToListAsync(cancellationToken))
                .GroupBy(x => Normalize(x.KumesYumurtaDurumAd))
                .ToDictionary(g => g.Key, g => g.First().KumesYumurtaDurumId);

            var kaliteDurumIdByName = (await _db.Set<KumesYumurtaKaliteDurum>()
                    .ToListAsync(cancellationToken))
                .GroupBy(x => Normalize(x.KumesYumurtaKaliteDurumAd))
                .ToDictionary(g => g.Key, g => g.First().KumesYumurtaKaliteDurumId);

            var saglamId = await GetOrCreateDurumAsync(
                durumIdByName, "Sağlam", cancellationToken);

            var kirikId = await GetOrCreateDurumAsync(
                durumIdByName, "Kırık", cancellationToken);

            var kirliId = await GetOrCreateDurumAsync(
                durumIdByName, "Kirli", cancellationToken);

            var kucukKirikId = await GetOrCreateDurumAsync(
                durumIdByName, "Küçük Kırık", cancellationToken);

            var fireId = await GetOrCreateDurumAsync(
                durumIdByName, "Fire", cancellationToken);

            // Ana excel'in ("Yumurtlama Kartı") "Yumurtlama " sayfasında
            // sonradan eklenen sütun — bkz. VGExcelKumesGunlukData.
            // LikidKabuksuzZar / VGYumurtlamaKartExcelReader. Eski kart
            // biçiminde (ör. "T1 127.PARTİ") hiç yok, o satırlarda
            // değer null gelir ve UpsertKumesVeriDurum hiçbir şey
            // yazmadan atlar.
            var likidKabuksuzZarId = await GetOrCreateDurumAsync(
                durumIdByName, "Likid - Kabuksuz Zar", cancellationToken);

            // Ana excel'in ("Yumurtlama Kartı") "Yumurtlama " sayfasında
            // sonradan eklenen "ZARLI YUMURTA (KG)" sütunu — bkz.
            // VGExcelKumesGunlukData.ZarliYumurtaKg.
            var zarliYumurtaKgId = await GetOrCreateDurumAsync(
                durumIdByName, "Zarlı Yumurta (Kg)", cancellationToken);

            // T14 135.PARTİ'de de şu an boş (hiç veri girilmemiş) ama
            // sütun kartta var — bkz. VGExcelKumesGunlukData.
            // MobaKirikDegeri/MobaKirliDegeri.
            var mobaKirikDegeriId = await GetOrCreateDurumAsync(
                durumIdByName, "MOBA Kırık Değeri", cancellationToken);

            var mobaKirliDegeriId = await GetOrCreateDurumAsync(
                durumIdByName, "MOBA Kirli Değeri (0,1,2,3)", cancellationToken);

            var gizliCatlakId = await GetOrCreateKaliteDurumAsync(
                kaliteDurumIdByName, "Gizli Çatlak", cancellationToken);

            var kabukDirenciId = await GetOrCreateKaliteDurumAsync(
                kaliteDurumIdByName, "Kabuk Direnci", cancellationToken);

            // Rahmiye "Veri Giriş Tablosu" gibi kaynaklarda sağlam/kırık/
            // kirli/fire kırılımı yok, sadece ham yumurta adedi var (bkz.
            // VGYumurtlamaExcelSourceService). Bu adet, kırılımı olmayan
            // günlerde HD/HH randıman hesaplanabilsin diye ayrıca
            // saklanıyor (VGYumurtlamaKaynakSatirRequest.ToplamYumurtaAdedi
            // fallback'i zaten bunu bekliyor).
            var hamYumurtaAdetiId = await GetOrCreateDurumAsync(
                durumIdByName, "Ham Yumurta Adedi", cancellationToken);

            var klavuzId = await GetOrCreateDurumAsync(
                durumIdByName, "Klavuz", cancellationToken);

            var smallId = await GetOrCreateDurumAsync(
                durumIdByName, "Small", cancellationToken);

            var mediumId = await GetOrCreateDurumAsync(
                durumIdByName, "Medium", cancellationToken);

            var largeId = await GetOrCreateDurumAsync(
                durumIdByName, "Large", cancellationToken);

            var jumboId = await GetOrCreateDurumAsync(
                durumIdByName, "Jumbo", cancellationToken);

            var sJumboId = await GetOrCreateDurumAsync(
                durumIdByName, "S.Jumbo", cancellationToken);

            var ciftSariId = await GetOrCreateDurumAsync(
                durumIdByName, "Çift Sarı", cancellationToken);


            // =====================================================
            // LOOKUP (HAYVAN DETAY) ID'LERİ
            // =====================================================

            var hayvanDetayIdByName = (await _db.Set<KumesHayvanDetay>()
                    .ToListAsync(cancellationToken))
                .GroupBy(x => Normalize(x.KumesHayvanDetayAdi))
                .ToDictionary(g => g.Key, g => g.First().KumesHayvanDetayId);

            var hayvanYasiId = await GetOrCreateHayvanDetayAsync(
                hayvanDetayIdByName, "Hayvan Yaşı (hf)", cancellationToken);

            var cekimSuresiId = await GetOrCreateHayvanDetayAsync(
                hayvanDetayIdByName, "Kümes Çekim Süre (dk)", cancellationToken);

            var saatteCekilenId = await GetOrCreateHayvanDetayAsync(
                hayvanDetayIdByName, "Saatte Çekilen Yum. Adet", cancellationToken);

            var fiiliHayvanId = await GetOrCreateHayvanDetayAsync(
                hayvanDetayIdByName, "Fiili Hayvan Sayısı", cancellationToken);

            var fiiliYumurtlayanHayvanId = await GetOrCreateHayvanDetayAsync(
                hayvanDetayIdByName, "Fiili Yumurtlayan Hayvan Sayısı", cancellationToken);


            // =====================================================
            // MEVCUT KAYITLAR (upsert için)
            // =====================================================

            var kumesIdler = rows
                .Select(x => kumeslerByNo[x.KumesNo.Trim().ToUpperInvariant()])
                .Distinct()
                .ToList();

            var minTarih = rows.Min(x => x.Tarih.Date);
            var maxTarih = rows.Max(x => x.Tarih.Date);

            var mevcutOlum = await _db.Set<Olum>()
                .Where(x =>
                    kumesIdler.Contains(x.KumeslerId) &&
                    x.OlumTarih >= minTarih && x.OlumTarih <= maxTarih)
                .ToListAsync(cancellationToken);

            var mevcutYem = await _db.Set<Yem>()
                .Where(x =>
                    kumesIdler.Contains(x.KumeslerId) &&
                    x.YemTarih >= minTarih && x.YemTarih <= maxTarih)
                .ToListAsync(cancellationToken);

            var mevcutSu = await _db.Set<Su>()
                .Where(x =>
                    kumesIdler.Contains(x.KumeslerId) &&
                    x.SuTarih >= minTarih && x.SuTarih <= maxTarih)
                .ToListAsync(cancellationToken);

            var mevcutYumurtaAgirlik = await _db.Set<YumurtaAgirlik>()
                .Where(x =>
                    kumesIdler.Contains(x.KumeslerId) &&
                    x.YumurtaAgirlikTarih >= minTarih && x.YumurtaAgirlikTarih <= maxTarih)
                .ToListAsync(cancellationToken);

            var mevcutUygulama = await _db.Set<Uygulama>()
                .Where(x =>
                    kumesIdler.Contains(x.KumeslerId) &&
                    x.UygulamalarTarih >= minTarih && x.UygulamalarTarih <= maxTarih)
                .ToListAsync(cancellationToken);

            var mevcutCanliAgirlik = await _db.Set<CanliAgirlik>()
                .Where(x =>
                    kumesIdler.Contains(x.KumeslerId) &&
                    x.CanliAgirlikTarih >= minTarih && x.CanliAgirlikTarih <= maxTarih)
                .ToListAsync(cancellationToken);

            var mevcutRandiman = await _db.Set<Randiman>()
                .Where(x =>
                    kumesIdler.Contains(x.KumeslerId) &&
                    x.RandimanTarih >= minTarih && x.RandimanTarih <= maxTarih)
                .ToListAsync(cancellationToken);

            var mevcutVbapRandiman = await _db.Set<VbapRandiman>()
                .Where(x =>
                    kumesIdler.Contains(x.KumeslerId) &&
                    x.VbapRandimanTarih >= minTarih && x.VbapRandimanTarih <= maxTarih)
                .ToListAsync(cancellationToken);

            var mevcutKumesVeri = await _db.Set<KumesVeri>()
                .Where(x =>
                    kumesIdler.Contains(x.KumeslerId) &&
                    x.Tarih >= minTarih &&
                    x.Tarih <= maxTarih)
                .ToListAsync(cancellationToken);


            var yazilanDeger = 0;

            foreach (var item in rows)
            {
                var kumesId = kumeslerByNo[item.KumesNo.Trim().ToUpperInvariant()];
                var tarih = item.Tarih.Date;

                if (UpsertSimple(
                        mevcutOlum, kumesId, tarih, item.Olum,
                        () => new Olum { KumeslerId = kumesId, OlumTarih = tarih },
                        (e, v) => e.OlumMiktar = v))
                    yazilanDeger++;

                if (UpsertSimple(
                        mevcutYem, kumesId, tarih, item.YemTuketim,
                        () => new Yem { KumeslerId = kumesId, YemTarih = tarih },
                        (e, v) => e.YemMiktar = v))
                    yazilanDeger++;

                if (UpsertSimple(
                        mevcutSu, kumesId, tarih, item.SuTuketim,
                        () => new Su { KumeslerId = kumesId, SuTarih = tarih },
                        (e, v) => e.SuMiktar = v))
                    yazilanDeger++;

                if (UpsertSimple(
                        mevcutYumurtaAgirlik, kumesId, tarih, item.YumurtaGramaji,
                        () => new YumurtaAgirlik { KumeslerId = kumesId, YumurtaAgirlikTarih = tarih },
                        (e, v) => e.YumurtaAgirlikMiktar = v))
                    yazilanDeger++;

                if (UpsertSimple(
                        mevcutCanliAgirlik, kumesId, tarih, item.CanliAgirlik,
                        () => new CanliAgirlik { KumeslerId = kumesId, CanliAgirlikTarih = tarih },
                        (e, v) => e.CanliAgirlikMiktar = v))
                    yazilanDeger++;

                if (UpsertSimple(
                        mevcutRandiman, kumesId, tarih, item.Randiman,
                        () => new Randiman { KumeslerId = kumesId, RandimanTarih = tarih },
                        (e, v) => e.RandimanMiktar = v))
                    yazilanDeger++;

                if (UpsertSimple(
                        mevcutVbapRandiman, kumesId, tarih, item.VbapRandiman,
                        () => new VbapRandiman { KumeslerId = kumesId, VbapRandimanTarih = tarih },
                        (e, v) => e.VbapRandimanMiktar = v))
                    yazilanDeger++;

                UpsertUygulamaTipi(
                    mevcutUygulama, kumesId, tarih, UygulamaTipiUygulama, item.Uygulama,
                    ref yazilanDeger);

                UpsertUygulamaTipi(
                    mevcutUygulama, kumesId, tarih, UygulamaTipiKumesPersoneli, item.KumesPersoneli,
                    ref yazilanDeger);

                UpsertUygulamaTipi(
                    mevcutUygulama, kumesId, tarih, UygulamaTipiYeminRasyonNo, item.YeminRasyonNo,
                    ref yazilanDeger);

                UpsertKumesVeriDurum(
                    mevcutKumesVeri, kumesId, tarih, saglamId, item.SaglamYumurta,
                    ref yazilanDeger);

                UpsertKumesVeriDurum(
                    mevcutKumesVeri, kumesId, tarih, kirikId, item.KirikYumurta,
                    ref yazilanDeger);

                UpsertKumesVeriDurum(
                    mevcutKumesVeri, kumesId, tarih, kirliId, item.KirliYumurta,
                    ref yazilanDeger);

                UpsertKumesVeriDurum(
                    mevcutKumesVeri, kumesId, tarih, kucukKirikId, item.KucukKirik,
                    ref yazilanDeger);

                UpsertKumesVeriDurum(
                    mevcutKumesVeri, kumesId, tarih, fireId, item.Fire,
                    ref yazilanDeger);

                UpsertKumesVeriDurum(
                    mevcutKumesVeri, kumesId, tarih, likidKabuksuzZarId, item.LikidKabuksuzZar,
                    ref yazilanDeger);

                UpsertKumesVeriDurum(
                    mevcutKumesVeri, kumesId, tarih, zarliYumurtaKgId, item.ZarliYumurtaKg,
                    ref yazilanDeger);

                UpsertKumesVeriDurum(
                    mevcutKumesVeri, kumesId, tarih, mobaKirikDegeriId, item.MobaKirikDegeri,
                    ref yazilanDeger);

                UpsertKumesVeriDurum(
                    mevcutKumesVeri, kumesId, tarih, mobaKirliDegeriId, item.MobaKirliDegeri,
                    ref yazilanDeger);

                UpsertKumesVeriKaliteDurum(
                    mevcutKumesVeri, kumesId, tarih, gizliCatlakId, item.GizliCatlak,
                    ref yazilanDeger);

                UpsertKumesVeriKaliteDurum(
                    mevcutKumesVeri, kumesId, tarih, kabukDirenciId, item.KabukDirenci,
                    ref yazilanDeger);

                UpsertKumesVeriDurum(
                    mevcutKumesVeri, kumesId, tarih, hamYumurtaAdetiId, item.YumurtaAdeti,
                    ref yazilanDeger);

                UpsertKumesVeriDurum(
                    mevcutKumesVeri, kumesId, tarih, klavuzId, item.Klavuz,
                    ref yazilanDeger);

                UpsertKumesVeriDurum(
                    mevcutKumesVeri, kumesId, tarih, smallId, item.Small,
                    ref yazilanDeger);

                UpsertKumesVeriDurum(
                    mevcutKumesVeri, kumesId, tarih, mediumId, item.Medium,
                    ref yazilanDeger);

                UpsertKumesVeriDurum(
                    mevcutKumesVeri, kumesId, tarih, largeId, item.Large,
                    ref yazilanDeger);

                UpsertKumesVeriDurum(
                    mevcutKumesVeri, kumesId, tarih, jumboId, item.Jumbo,
                    ref yazilanDeger);

                UpsertKumesVeriDurum(
                    mevcutKumesVeri, kumesId, tarih, sJumboId, item.SJumbo,
                    ref yazilanDeger);

                UpsertKumesVeriDurum(
                    mevcutKumesVeri, kumesId, tarih, ciftSariId, item.CiftSari,
                    ref yazilanDeger);

                UpsertKumesVeriHayvanDetay(
                    mevcutKumesVeri, kumesId, tarih, hayvanYasiId, item.HayvanYasiHafta,
                    ref yazilanDeger);

                UpsertKumesVeriHayvanDetay(
                    mevcutKumesVeri, kumesId, tarih, cekimSuresiId, item.CekimSuresiDk,
                    ref yazilanDeger);

                UpsertKumesVeriHayvanDetay(
                    mevcutKumesVeri, kumesId, tarih, saatteCekilenId, item.SaatteCekilenYumurtaAdet,
                    ref yazilanDeger);

                UpsertKumesVeriHayvanDetay(
                    mevcutKumesVeri, kumesId, tarih, fiiliHayvanId, item.FiiliHayvanSayisi,
                    ref yazilanDeger);

                UpsertKumesVeriHayvanDetay(
                    mevcutKumesVeri, kumesId, tarih, fiiliYumurtlayanHayvanId, item.FiiliYumurtlayanHayvanSayisi,
                    ref yazilanDeger);
            }

            result.YazilanDegerSayisi = yazilanDeger;

            await _db.SaveChangesAsync(cancellationToken);

            return result;
        }


        // =====================================================
        // YARDIMCI METODLAR
        // =====================================================

        private bool UpsertSimple<TEntity>(
            List<TEntity> mevcutListe,
            int kumesId,
            DateTime tarih,
            decimal? deger,
            Func<TEntity> factory,
            Action<TEntity, decimal> setValue)
            where TEntity : class
        {
            if (!deger.HasValue)
                return false;

            var existing = mevcutListe
                .FirstOrDefault(x => MatchesKumesTarih(x, kumesId, tarih));

            if (existing != null)
            {
                setValue(existing, deger.Value);
                return true;
            }

            var entity = factory();
            setValue(entity, deger.Value);

            _db.Set<TEntity>().Add(entity);
            mevcutListe.Add(entity);

            return true;
        }

        /// <summary>
        /// "Uygulama" tablosundaki bir satırı Tip'e göre upsert eder —
        /// bkz. UygulamaTipiUygulama/KumesPersoneli/YeminRasyonNo sabitleri
        /// üzerindeki not. "Uygulama" tipinde eski (Tip NULL) kayıtlarla
        /// da eşleşsin diye NULL ayrıca Uygulama ile eşdeğer sayılır.
        /// </summary>
        private void UpsertUygulamaTipi(
            List<Uygulama> mevcutListe,
            int kumesId,
            DateTime tarih,
            string tip,
            string? metin,
            ref int yazilanDeger)
        {
            if (string.IsNullOrWhiteSpace(metin))
                return;

            var existing = mevcutListe.FirstOrDefault(x =>
                x.KumeslerId == kumesId &&
                x.UygulamalarTarih.Date == tarih &&
                (x.Tip == tip ||
                 (tip == UygulamaTipiUygulama && x.Tip == null)));

            if (existing != null)
            {
                existing.UygulamaAciklama = metin;
                existing.Tip = tip;
            }
            else
            {
                var entity = new Uygulama
                {
                    KumeslerId = kumesId,
                    UygulamalarTarih = tarih,
                    UygulamaAciklama = metin,
                    Tip = tip
                };

                _db.Set<Uygulama>().Add(entity);
                mevcutListe.Add(entity);
            }

            yazilanDeger++;
        }

        private static bool MatchesKumesTarih<TEntity>(
            TEntity entity, int kumesId, DateTime tarih)
        {
            return entity switch
            {
                Olum e => e.KumeslerId == kumesId && e.OlumTarih.Date == tarih,
                Yem e => e.KumeslerId == kumesId && e.YemTarih.Date == tarih,
                Su e => e.KumeslerId == kumesId && e.SuTarih.Date == tarih,
                YumurtaAgirlik e => e.KumeslerId == kumesId && e.YumurtaAgirlikTarih.Date == tarih,
                CanliAgirlik e => e.KumeslerId == kumesId && e.CanliAgirlikTarih.Date == tarih,
                Randiman e => e.KumeslerId == kumesId && e.RandimanTarih.Date == tarih,
                VbapRandiman e => e.KumeslerId == kumesId && e.VbapRandimanTarih.Date == tarih,
                _ => false
            };
        }

        private void UpsertKumesVeriDurum(
            List<KumesVeri> mevcutListe,
            int kumesId,
            DateTime tarih,
            int durumId,
            decimal? deger,
            ref int yazilanDeger)
        {
            if (!deger.HasValue)
                return;

            var existing = mevcutListe.FirstOrDefault(x =>
                x.KumeslerId == kumesId &&
                x.Tarih.Date == tarih &&
                x.KumesYumurtaDurumId == durumId);

            if (existing != null)
            {
                existing.KumesYumurtaDurumDeger = deger.Value;
            }
            else
            {
                var entity = new KumesVeri
                {
                    KumeslerId = kumesId,
                    Tarih = tarih,
                    KumesYumurtaDurumId = durumId,
                    KumesYumurtaDurumDeger = deger.Value
                };

                _db.Set<KumesVeri>().Add(entity);
                mevcutListe.Add(entity);
            }

            yazilanDeger++;
        }

        private void UpsertKumesVeriKaliteDurum(
            List<KumesVeri> mevcutListe,
            int kumesId,
            DateTime tarih,
            int kaliteDurumId,
            decimal? deger,
            ref int yazilanDeger)
        {
            if (!deger.HasValue)
                return;

            var existing = mevcutListe.FirstOrDefault(x =>
                x.KumeslerId == kumesId &&
                x.Tarih.Date == tarih &&
                x.KumesYumurtaKaliteDurumId == kaliteDurumId);

            if (existing != null)
            {
                existing.KumesYumurtaKaliteDurumDeger = deger.Value;
            }
            else
            {
                var entity = new KumesVeri
                {
                    KumeslerId = kumesId,
                    Tarih = tarih,
                    KumesYumurtaKaliteDurumId = kaliteDurumId,
                    KumesYumurtaKaliteDurumDeger = deger.Value
                };

                _db.Set<KumesVeri>().Add(entity);
                mevcutListe.Add(entity);
            }

            yazilanDeger++;
        }

        private void UpsertKumesVeriHayvanDetay(
            List<KumesVeri> mevcutListe,
            int kumesId,
            DateTime tarih,
            int hayvanDetayId,
            decimal? deger,
            ref int yazilanDeger)
        {
            if (!deger.HasValue)
                return;

            var existing = mevcutListe.FirstOrDefault(x =>
                x.KumeslerId == kumesId &&
                x.Tarih.Date == tarih &&
                x.KumesHayvanDetayId == hayvanDetayId);

            if (existing != null)
            {
                existing.KumesHayvanDetayDeger = deger.Value;
            }
            else
            {
                var entity = new KumesVeri
                {
                    KumeslerId = kumesId,
                    Tarih = tarih,
                    KumesHayvanDetayId = hayvanDetayId,
                    KumesHayvanDetayDeger = deger.Value
                };

                _db.Set<KumesVeri>().Add(entity);
                mevcutListe.Add(entity);
            }

            yazilanDeger++;
        }

        private async Task<int> GetOrCreateDurumAsync(
            Dictionary<string, int> cache,
            string displayName,
            CancellationToken cancellationToken)
        {
            var key = Normalize(displayName);

            if (cache.TryGetValue(key, out var id))
                return id;

            var entity = new KumesYumurtaDurum
            {
                KumesYumurtaDurumAd = displayName
            };

            _db.Set<KumesYumurtaDurum>().Add(entity);
            await _db.SaveChangesAsync(cancellationToken);

            cache[key] = entity.KumesYumurtaDurumId;

            return entity.KumesYumurtaDurumId;
        }

        private async Task<int> GetOrCreateKaliteDurumAsync(
            Dictionary<string, int> cache,
            string displayName,
            CancellationToken cancellationToken)
        {
            var key = Normalize(displayName);

            if (cache.TryGetValue(key, out var id))
                return id;

            var entity = new KumesYumurtaKaliteDurum
            {
                KumesYumurtaKaliteDurumAd = displayName
            };

            _db.Set<KumesYumurtaKaliteDurum>().Add(entity);
            await _db.SaveChangesAsync(cancellationToken);

            cache[key] = entity.KumesYumurtaKaliteDurumId;

            return entity.KumesYumurtaKaliteDurumId;
        }

        private async Task<int> GetOrCreateHayvanDetayAsync(
            Dictionary<string, int> cache,
            string displayName,
            CancellationToken cancellationToken)
        {
            var key = Normalize(displayName);

            if (cache.TryGetValue(key, out var id))
                return id;

            var entity = new KumesHayvanDetay
            {
                KumesHayvanDetayAdi = displayName
            };

            _db.Set<KumesHayvanDetay>().Add(entity);
            await _db.SaveChangesAsync(cancellationToken);

            cache[key] = entity.KumesHayvanDetayId;

            return entity.KumesHayvanDetayId;
        }

        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Trim()
                .ToUpperInvariant()
                // ToUpperInvariant() Türkçe noktasız "ı"yı büyütmüyor — bkz.
                // VGYumurtlamaQueryService.Normalize üzerindeki not.
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
