using HegCoolingScada.Application.Interfaces;
using HegCoolingScada.Domain.DbViewModels;
using HegCoolingScada.Domain.Interfaces;
using HegCoolingScada.Domain.Models;
using HegCoolingScada.Domain.Models.ReportModels;

namespace HegCoolingScada.Application.Services
{
    public class HegSogutmaRaporAppService : IHegSogutmaRaporAppService
    {
        // Günlük/aylık dilimler Europe/Istanbul takvim sınırlarına göre hizalanır
        // (gün/ay başı 00:00 İstanbul, UTC'de değil); bkz. HegSogutmaRepository'deki
        // aynı amaçlı kompresör bucket hizalaması.
        private static readonly TimeZoneInfo IstanbulTimeZone = ResolveIstanbulTimeZone();

        private readonly IHegSogutmaRepository _repository;

        public HegSogutmaRaporAppService(IHegSogutmaRepository repository)
        {
            _repository = repository;
        }

        private static TimeZoneInfo ResolveIstanbulTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            }
        }

        private static DateTime IstanbulGunBasiUtc(DateTime utcZaman)
        {
            var istanbulZamani = TimeZoneInfo.ConvertTimeFromUtc(utcZaman, IstanbulTimeZone);
            var gunBasiIstanbul = DateTime.SpecifyKind(istanbulZamani.Date, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(gunBasiIstanbul, IstanbulTimeZone);
        }

        private static DateTime IstanbulAyBasiUtc(DateTime utcZaman)
        {
            var istanbulZamani = TimeZoneInfo.ConvertTimeFromUtc(utcZaman, IstanbulTimeZone);
            var ayBasiIstanbul = DateTime.SpecifyKind(new DateTime(istanbulZamani.Year, istanbulZamani.Month, 1), DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(ayBasiIstanbul, IstanbulTimeZone);
        }

        private static DateTime IstanbulSonrakiGunBasiUtc(DateTime gunBasiUtc)
        {
            var istanbulZamani = TimeZoneInfo.ConvertTimeFromUtc(gunBasiUtc, IstanbulTimeZone);
            var sonrakiGunBasiIstanbul = DateTime.SpecifyKind(istanbulZamani.Date.AddDays(1), DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(sonrakiGunBasiIstanbul, IstanbulTimeZone);
        }

        private static DateTime IstanbulSonrakiAyBasiUtc(DateTime ayBasiUtc)
        {
            var istanbulZamani = TimeZoneInfo.ConvertTimeFromUtc(ayBasiUtc, IstanbulTimeZone);
            var sonrakiAyBasiIstanbul = DateTime.SpecifyKind(new DateTime(istanbulZamani.Year, istanbulZamani.Month, 1).AddMonths(1), DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(sonrakiAyBasiIstanbul, IstanbulTimeZone);
        }

        public Task<List<Oda>> GetOdalarAsync() => _repository.GetAllOdalarAsync();

        public Task<List<Kompresor>> GetKompresorlerAsync() => _repository.GetAllKompresorlerAsync();

        // Önceden vanalar/odalar/vana_anlik ayrı ayrı çekilip bellekte join'leniyordu
        // (3 sıralı DB round-trip). Bu tablolar uzak sunucuda barındığından, seçim
        // listesi her açılışında bu 3 round-trip'in gecikmesi doğrudan hissediliyordu.
        // Artık tek bir LEFT JOIN sorgusuyla (bkz. GetVanalarWithDurumAsync) tek
        // round-trip'e indiriliyor.
        public Task<List<VanaViewModel>> GetVanalarAsync() => _repository.GetVanalarWithDurumAsync();

        public async Task<List<VanaDurumRaporSatiri>> GetVanaDurumRaporuAsync(DateTime baslangic, DateTime bitis, string periyot, IReadOnlyCollection<short>? vanaIds = null)
        {
            if (bitis <= baslangic)
            {
                throw new ArgumentException("Bitiş tarihi başlangıçtan sonra olmalıdır.");
            }

            var vanalar = await _repository.GetAllVanalarAsync();
            if (vanaIds is { Count: > 0 })
            {
                var vanaIdSet = vanaIds.ToHashSet();
                vanalar = vanalar.Where(x => vanaIdSet.Contains(x.Id)).ToList();
            }

            var odaLookup = (await _repository.GetAllOdalarAsync()).ToDictionary(x => x.Id);
            var anlikLookup = (await _repository.GetAllVanaAnlikAsync()).ToDictionary(x => x.VanaId);

            var normalizePeriyot = periyot?.ToLowerInvariant();

            // Seçilen aralık, her biri ayrı bir rapor satırı üretecek şekilde periyoda göre
            // (saatlik/günlük/aylık) dilimlere ayrılır (son dilim küsuratlı kalabilir).
            // Günlük/aylık dilimler, seçilen başlangıç saatinden değil İstanbul takvim
            // gün/ay başından başlar (örn. 16:00'da başlayan seçim, günlükte o günün
            // 00:00'ından itibaren hesaplanır); aksi halde her dilim sadece "başlangıç +
            // 1 gün/ay" olur ve gün/ay sınırlarını yansıtmaz. Özet ise hiçbir hizalama
            // yapmadan tam olarak seçilen [baslangic, bitis) aralığını tek dilimde,
            // vana başına tek satır olarak döner.
            var saatDilimleri = new List<(DateTime Baslangic, DateTime Bitis)>();
            if (normalizePeriyot == "ozet")
            {
                saatDilimleri.Add((baslangic, bitis));
            }
            else if (normalizePeriyot is "gunluk" or "aylik")
            {
                var ilkDilimBaslangic = normalizePeriyot == "aylik" ? IstanbulAyBasiUtc(baslangic) : IstanbulGunBasiUtc(baslangic);
                for (var dilimBaslangic = ilkDilimBaslangic; dilimBaslangic < bitis;)
                {
                    var dilimBitis = normalizePeriyot == "aylik"
                        ? IstanbulSonrakiAyBasiUtc(dilimBaslangic)
                        : IstanbulSonrakiGunBasiUtc(dilimBaslangic);
                    if (dilimBitis > bitis) dilimBitis = bitis;
                    saatDilimleri.Add((dilimBaslangic, dilimBitis));
                    dilimBaslangic = dilimBitis;
                }
            }
            else
            {
                for (var dilimBaslangic = baslangic; dilimBaslangic < bitis;)
                {
                    var dilimBitis = dilimBaslangic.AddHours(1);
                    if (dilimBitis > bitis) dilimBitis = bitis;
                    saatDilimleri.Add((dilimBaslangic, dilimBitis));
                    dilimBaslangic = dilimBitis;
                }
            }

            var sonuc = new List<VanaDurumRaporSatiri>(vanalar.Count * saatDilimleri.Count);

            foreach (var vana in vanalar)
            {
                var oda = vana.OdaId.HasValue && odaLookup.TryGetValue(vana.OdaId.Value, out var o) ? o : null;
                anlikLookup.TryGetValue(vana.Id, out var anlik);

                foreach (var dilim in saatDilimleri)
                {
                    var dilimToplamSaniye = (decimal)(dilim.Bitis - dilim.Baslangic).TotalSeconds;

                    // vana_acik_saniye() bu vana/dilim için veri bulamazsa NULL döner;
                    // bu durumda açık/kapalı/yüzde alanlarını "veri yok" olarak null bırakıyoruz.
                    var acikSaniye = await _repository.GetVanaAcikSaniyeAsync(vana.Id, dilim.Baslangic, dilim.Bitis);
                    var kapaliSaniye = acikSaniye.HasValue ? Math.Max(0, dilimToplamSaniye - acikSaniye.Value) : (decimal?)null;

                    sonuc.Add(new VanaDurumRaporSatiri
                    {
                        VanaId = vana.Id,
                        OdaId = vana.OdaId,
                        OdaAdi = oda?.Ad,
                        OdaKat = oda?.Kat,
                        EkipmanKodu = vana.EkipmanKodu,
                        Aciklama = vana.Aciklama,
                        GuncelDurumAcik = anlik?.Durum,
                        GuncelDurumBaslangici = anlik?.DurumBaslangici,
                        AralikBaslangic = dilim.Baslangic,
                        AralikBitis = dilim.Bitis,
                        AcikSaniye = acikSaniye,
                        KapaliSaniye = kapaliSaniye,
                        AcikYuzde = acikSaniye.HasValue && dilimToplamSaniye > 0 ? Math.Round(acikSaniye.Value / dilimToplamSaniye * 100, 2) : (decimal?)null,
                    });
                }
            }

            return sonuc
                .OrderBy(x => x.OdaKat)
                .ThenBy(x => x.OdaAdi)
                .ThenBy(x => x.EkipmanKodu)
                .ThenBy(x => x.AralikBaslangic)
                .ToList();
        }

        public async Task<List<KompresorRaporSatiri>> GetKompresorRaporuAsync(DateTime baslangic, DateTime bitis, string periyot, IReadOnlyCollection<short>? kompresorIds = null)
        {
            if (bitis <= baslangic)
            {
                throw new ArgumentException("Bitiş tarihi başlangıçtan sonra olmalıdır.");
            }

            var kompresorLookup = (await _repository.GetAllKompresorlerAsync()).ToDictionary(x => x.Id);
            var normalizePeriyot = periyot?.ToLowerInvariant();

            if (normalizePeriyot is "gunluk" or "aylik")
            {
                // Aylık/günlük her zaman İstanbul takvim ay/gün başından itibaren
                // hesaplanır (baslangic ay/gün içinde bir saat olsa bile) — kullanıcı
                // seçtiği ay/günün tamamını görür. Satırlar saatlik bucketlardan
                // ağırlıklı ortalama yöntemiyle türetilir; bkz. SatirOlustur.
                var efektifBaslangic = normalizePeriyot == "gunluk"
                    ? IstanbulGunBasiUtc(baslangic)
                    : IstanbulAyBasiUtc(baslangic);

                var saatlikBucketlar = (await _repository.GetKompresorOkumaSaatlikAsync(efektifBaslangic, bitis, kompresorIds)).Cast<KompresorOkumaBucketBase>().ToList();

                return PeriyotSatirlariOlustur(saatlikBucketlar, normalizePeriyot, kompresorLookup);
            }

            if (normalizePeriyot == "ozet")
            {
                // Özet, tam olarak seçilen [baslangic, bitis) aralığını yansıtır —
                // ay/gün başına hizalanmaz; saatlik bucketlar üzerinden aynı ağırlıklı
                // ortalama yöntemiyle kompresör başına tek satıra indirgenir.
                var saatlikBucketlar = (await _repository.GetKompresorOkumaSaatlikAsync(baslangic, bitis, kompresorIds)).Cast<KompresorOkumaBucketBase>().ToList();
                return OzetSatirlariOlustur(saatlikBucketlar, baslangic, kompresorLookup);
            }

            var bucketlar = (await _repository.GetKompresorOkumaSaatlikAsync(baslangic, bitis, kompresorIds)).Cast<KompresorOkumaBucketBase>().ToList();

            return bucketlar
                .Select(b =>
                {
                    kompresorLookup.TryGetValue(b.KompresorId, out var kompresor);

                    return new KompresorRaporSatiri
                    {
                        KompresorId = b.KompresorId,
                        TagNo = kompresor?.TagNo,
                        Ad = kompresor?.Ad,
                        Bucket = b.Bucket,
                        OrtKapasite = b.OrtKapasite,
                        MinKapasite = b.MinKapasite,
                        MaksKapasite = b.MaksKapasite,
                        CalismaSaatiBaslangicHam = b.SaatBaslangicHam,
                        CalismaSaatiBitisHam = b.SaatBitisHam,
                        CalismaSaatiFarki = b.SaatFark,
                        OrnekSayisi = b.OrnekSayisi,
                    };
                })
                .OrderBy(x => x.TagNo)
                .ThenBy(x => x.Bucket)
                .ToList();
        }

        // Seçilen aralıktaki saatlik bucketları kompresör başına tek satıra indirger:
        // örnek sayısı toplanır, ortalama kapasite örnek-ağırlıklı hesaplanır, min/maks
        // kapasite aralığın tamamından alınır, çalışma saati ham değerleri aralığın ilk
        // ve son (null olmayan) bucket'ından okunur.
        private static List<KompresorRaporSatiri> OzetSatirlariOlustur(
            List<KompresorOkumaBucketBase> bucketlar,
            DateTime baslangic,
            Dictionary<short, Kompresor> kompresorLookup)
        {
            return bucketlar
                .GroupBy(x => x.KompresorId)
                .Select(g => SatirOlustur(g.Key, baslangic, g.OrderBy(x => x.Bucket).ToList(), kompresorLookup))
                .OrderBy(x => x.TagNo)
                .ToList();
        }

        // Seçilen aralıktaki saatlik bucketları, İstanbul takvim gün/ay başına göre
        // gruplayıp her gruba OzetSatirlariOlustur ile aynı yöntemi uygular. Böylece
        // günlük/aylık raporun ilk (ve gerekirse son) satırı, baslangic/bitis bir
        // gün/ay ortasında olsa bile yalnızca gerçekten seçilen aralığı yansıtır.
        private static List<KompresorRaporSatiri> PeriyotSatirlariOlustur(
            List<KompresorOkumaBucketBase> saatlikBucketlar,
            string periyot,
            Dictionary<short, Kompresor> kompresorLookup)
        {
            Func<DateTime, DateTime> bucketBasi = periyot == "aylik" ? IstanbulAyBasiUtc : IstanbulGunBasiUtc;

            return saatlikBucketlar
                .GroupBy(x => (x.KompresorId, Bucket: bucketBasi(x.Bucket)))
                .Select(g => SatirOlustur(g.Key.KompresorId, g.Key.Bucket, g.OrderBy(x => x.Bucket).ToList(), kompresorLookup))
                .OrderBy(x => x.TagNo)
                .ThenBy(x => x.Bucket)
                .ToList();
        }

        private static KompresorRaporSatiri SatirOlustur(
            short kompresorId,
            DateTime bucket,
            List<KompresorOkumaBucketBase> siraliListe,
            Dictionary<short, Kompresor> kompresorLookup)
        {
            kompresorLookup.TryGetValue(kompresorId, out var kompresor);

            var ornekSayisiToplam = siraliListe.Any(x => x.OrnekSayisi.HasValue)
                ? siraliListe.Sum(x => x.OrnekSayisi ?? 0)
                : (long?)null;

            var baslangicHam = siraliListe.Select(x => x.SaatBaslangicHam).FirstOrDefault(x => x.HasValue);
            var bitisHam = siraliListe.Select(x => x.SaatBitisHam).LastOrDefault(x => x.HasValue);

            var agirlikliPay = 0m;
            var agirlikliPayda = 0m;
            foreach (var b in siraliListe)
            {
                if (b.OrtKapasite is null || b.OrnekSayisi is not > 0) continue;
                agirlikliPay += b.OrtKapasite.Value * b.OrnekSayisi.Value;
                agirlikliPayda += b.OrnekSayisi.Value;
            }

            var ortKapasite = agirlikliPayda > 0
                ? agirlikliPay / agirlikliPayda
                : siraliListe.Select(x => x.OrtKapasite).Average();

            return new KompresorRaporSatiri
            {
                KompresorId = kompresorId,
                TagNo = kompresor?.TagNo,
                Ad = kompresor?.Ad,
                Bucket = bucket,
                OrtKapasite = ortKapasite.HasValue ? Math.Round(ortKapasite.Value, 2) : (decimal?)null,
                MinKapasite = siraliListe.Select(x => x.MinKapasite).Min(),
                MaksKapasite = siraliListe.Select(x => x.MaksKapasite).Max(),
                CalismaSaatiBaslangicHam = baslangicHam,
                CalismaSaatiBitisHam = bitisHam,
                CalismaSaatiFarki = baslangicHam.HasValue && bitisHam.HasValue ? bitisHam - baslangicHam : (decimal?)null,
                OrnekSayisi = ornekSayisiToplam,
            };
        }
    }
}
