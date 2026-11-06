using PoultryHouseScada.Application.Interfaces;
using PoultryHouseScada.Application.RequestModels;
using PoultryHouseScada.Application.ViewModels;
using PoultryHouseScada.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace PoultryHouseScada.Infrastructure.Services.VG
{
    /// <summary>
    /// Bkz. IVGYumurtlamaGenelVeriService — mevcut hesaplama motorunu
    /// (IVGYumurtlamaQueryService/IVGYumurtlamaService, aynı GÜN/HAFTA
    /// mantığı) kullanarak Verim Gıda'nın beklediği haftalara gruplanmış
    /// JSON paketini üretir.
    /// </summary>
    public sealed class VGYumurtlamaGenelVeriService : IVGYumurtlamaGenelVeriService
    {
        private readonly KumesScadaDbContext _db;

        private readonly IVGYumurtlamaQueryService _queryService;

        private readonly IVgYumurtlamaDonemAppService _donemService;

        public VGYumurtlamaGenelVeriService(
            KumesScadaDbContext db,
            IVGYumurtlamaQueryService queryService,
            IVgYumurtlamaDonemAppService donemService)
        {
            _db = db;
            _queryService = queryService;
            _donemService = donemService;
        }

        public async Task<VGYumurtlamaGenelVeriViewModel> GetAsync(
            int kumesId,
            CancellationToken cancellationToken = default)
        {
            // DİKKAT: "id" burada Kumesler.KumesId (genel Kumes tablosuna
            // FK — PLC/dönem bilgisi/nekropsi gibi uygulamanın geri
            // kalanının kullandığı id) DEĞİL, Kumesler.Id (bu VG
            // tablosunun kendi PK'sı) ile eşleştirilir — Verim Gıda'nın
            // dış entegrasyon sözleşmesi bu id'yi bekliyor.
            var kumes = await _db.Set<Kumesler>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == kumesId, cancellationToken);

            if (kumes == null || string.IsNullOrWhiteSpace(kumes.KumesNo))
                throw new Exception($"{kumesId} id'li kümes bulunamadı.");

            var donem = await _donemService.GetLatestByKumesNo(kumes.KumesNo, cancellationToken);

            if (donem == null)
                throw new Exception(
                    $"{kumes.KumesNo} için dönem bilgisi (kümese giriş tarihi/başlangıç hayvan " +
                    "sayısı) kaydedilmemiş. Önce \"Yumurtlama Kartı Excel'ini Oluştur\" formundan " +
                    "elle girilmeli ya da \"Geçmiş Veri Yükle\" ile ana excel'den otomatik türetilmeli.");

            var hesap = await _queryService.GetAsync(
                new VGYumurtlamaQueryRequest
                {
                    KumesNo = kumes.KumesNo,
                    Year = null,
                    Irk = donem.Irk,
                    KumeseGirisTarihi = donem.GirisTarihi,
                    BaslangicHayvanSayisi = donem.BaslangicHayvanSayisi,
                    GirisYasiGun = donem.GirisYasiGun,
                    GirisHaftasi = donem.GirisHaftasi,
                },
                cancellationToken);

            var kardesKumesler = await _db.Set<Kumesler>()
                .AsNoTracking()
                .Where(x =>
                    x.LokasyonId == kumes.LokasyonId &&
                    x.Silindi != true)
                .OrderBy(x => x.KumesName)
                .ToListAsync(cancellationToken);

            return new VGYumurtlamaGenelVeriViewModel
            {
                Kumes = ToKumesViewModel(kumes),

                GenelVeri = new VGYumurtlamaGenelVeriGenelViewModel
                {
                    Kumesler = kardesKumesler.Select(ToKumesViewModel).ToList(),
                    Irk = donem.Irk,
                    KumeseGirisTarihi = donem.GirisTarihi.ToString("dd.MM.yyyy"),
                    Miktar = donem.BaslangicHayvanSayisi,
                },

                Haftalar = BuildHaftalar(hesap, donem.BaslangicHayvanSayisi),
            };
        }

        private static VGYumurtlamaGenelVeriKumesViewModel ToKumesViewModel(Kumesler kumes)
        {
            return new VGYumurtlamaGenelVeriKumesViewModel
            {
                // Bkz. GetAsync üzerindeki not — Kumesler.KumesId değil,
                // bu VG tablosunun kendi PK'sı (Verim Gıda sözleşmesi bunu bekliyor).
                Id = kumes.Id,
                Ad = kumes.KumesName ?? kumes.KumesNo ?? string.Empty,
            };
        }

        private static List<VGYumurtlamaGenelVeriHaftaViewModel> BuildHaftalar(
            VGYumurtlamaViewModel hesap,
            decimal baslangicHayvanSayisi)
        {
            var result = new List<VGYumurtlamaGenelVeriHaftaViewModel>();

            // T14 135.PARTİ gibi yeni kart biçiminde "Fire"/"Küçük Kırık"
            // artık "Likid"/"Zar Çatlağı" diye adlandırılıyor — aynı alan
            // (bkz. VGYumurtlamaExcelGenerator'daki not), ama dış
            // entegrasyon sözleşmesinde HER İKİ isimli alanı da aynı
            // değerle doldurmak yerine, kartın kendi biçimine göre sadece
            // GEÇERLİ olanı dolduruyoruz — diğeri 0 kalır (alan tipleri
            // sözleşme gereği non-nullable decimal, null gönderilemez).
            // Biçim, GÜN/HAFTA BAZINDA (bkz. aşağıdaki gunYeniFormat/
            // haftaYeniFormat) yeni sütunlardan (Likid-Kabuksuz Zar/
            // Zarlı Yumurta/MOBA/Kümes Personeli/Yemin Rasyon No)
            // herhangi birinin O GÜN/HAFTA için görülüp görülmediğine
            // bakılarak anlaşılıyor — KÜMESİN TÜM TARİHÇESİ üzerinden TEK
            // bir global bayrak KULLANILMIYOR: aynı kümes farklı
            // partilerde (dönemlerde) hem eski hem yeni kart biçimiyle
            // yüklenmiş olabilir (bkz. proje notları — eski biçimle
            // "Geçmiş Veri Yükle" ile yüklenen bir parti, kümes DAHA
            // ÖNCE/SONRA yeni biçimle de yüklendiyse yanlışlıkla Zar
            // Çatlağı/Likit diye etiketleniyordu).

            var haftalikByHafta = hesap.HaftalikVeriler.ToDictionary(x => x.Hafta);

            // Veritabanında (ör. test/demo amaçlı önceden doldurulmuş
            // takvim günleri yüzünden) gerçek veriden çok daha ileri
            // tarihlere kadar boş/sıfır satırlar bulunabiliyor — dış
            // entegrasyon tüketicisine anlamsız onlarca boş hafta
            // göndermemek için, son GERÇEK veri içeren haftadan SONRASI
            // kesiliyor. "Gerçek veri" = o hafta sonunda kümeste
            // GERÇEKTEN tavuk var (DonemSonuTavukSayisi > 0) VE
            // ölüm/yem/su/yumurta üretiminden en az biri sıfırdan farklı.
            // Sadece ikinci şart yeterli değil: tavuk sayısı 0 olduğu
            // halde (kümes henüz dolmamış/boşalmış) su/yem gibi alanlarda
            // kalıntı sıfır-olmayan değerler bulunabiliyor ve bu haftaları
            // da "dolu" gibi gösteriyordu.
            var sonDoluHafta = hesap.HaftalikVeriler
                .Where(x =>
                    x.DonemSonuTavukSayisi > 0 &&
                    (x.ToplamOlum > 0 ||
                     x.ToplamYemKg > 0 ||
                     x.ToplamSuLitre > 0 ||
                     x.ToplamYumurta > 0))
                .Select(x => (int?)x.Hafta)
                .DefaultIfEmpty(null)
                .Max();

            var gunlukGruplari = hesap.GunlukVeriler
                .GroupBy(x => x.Hafta)
                .Where(g => sonDoluHafta == null || g.Key <= sonDoluHafta.Value)
                .OrderBy(x => x.Key);

            // "tavuk başına yumurta toplamı" gerçek ana excel kartında
            // kümülatif — her haftanın "haftalık yumurta tavuk başına"
            // değeri bir önceki haftanın kümülatif toplamına eklenerek
            // ilerliyor (bkz. T1 127.PARTİ SÜPER NİCK#M2.xls üzerinde
            // doğrulanan desen: 0.0857 -> 0.9236 -> 5.5095 -> 10.6915 -> ...).
            var tavukBasinaKumulatif = 0m;

            // Her haftanın "haftalık yumurta tavuk başına" değeri, o
            // haftanın KENDİ (ölümlerle azalan) tavuk sayısına değil, 18.
            // haftanın bitişindeki/19. haftanın başındaki SABİT tavuk
            // sayısına bölünüyor — gerçek excel kartında bu, tüm haftalarca
            // kullanılan MUTLAK bir hücre referansı ($J$39). Tüm VG kümes/
            // partileri için 19. hafta başlangıcı sabit referans kabul
            // ediliyor (bkz. T1 127.PARTİ SÜPER NİCK#M2.xls doğrulaması:
            // hafta20 11669/136107=0.08573, hafta22 114041/136107=0.83788,
            // 136107 = hafta18 DonemSonuTavukSayisi).
            var tavukBasinaReferansSayisi =
                haftalikByHafta.TryGetValue(18, out var referansHafta) && referansHafta.DonemSonuTavukSayisi > 0
                    ? referansHafta.DonemSonuTavukSayisi
                    : hesap.HaftalikVeriler
                        .Where(x => x.DonemSonuTavukSayisi > 0)
                        .OrderBy(x => x.Hafta)
                        .Select(x => (decimal?)x.DonemSonuTavukSayisi)
                        .FirstOrDefault() ?? baslangicHayvanSayisi;

            foreach (var grup in gunlukGruplari)
            {
                if (!haftalikByHafta.TryGetValue(grup.Key, out var haftalik))
                    continue;

                var gunler = new List<VGYumurtlamaGenelVeriGunViewModel>();

                var yumurtaBasina = YumurtaBasinaHesapla(haftalik, tavukBasinaReferansSayisi);

                if (yumurtaBasina.HasValue)
                    tavukBasinaKumulatif += yumurtaBasina.Value;

                var yumurtaBasinaToplamKumulatif = tavukBasinaKumulatif > 0
                    ? tavukBasinaKumulatif
                    : (decimal?)null;

                // "Yem (gr)" kriterinin hafta_degeri'i için — bkz.
                // KriterVeHaftaDegeri üzerindeki not.
                var ortalamaTavukSayisi = grup.Average(x => x.TavukSayisi);

                foreach (var gun in grup.OrderBy(x => x.Tarih))
                {
                    // Pazartesi=1 ... Pazar=7 — gerçek ana excel kartındaki
                    // "KRİTER" rotasyonu haftanın gününe göre belirlenir
                    // (bkz. VGYumurtlamaKartExcelReader üzerindeki not).
                    var haftaninGunu = ((int)gun.Tarih.DayOfWeek + 6) % 7 + 1;

                    var (kriter, haftaDegeri) = KriterVeHaftaDegeri(
                        haftaninGunu, haftalik, baslangicHayvanSayisi, ortalamaTavukSayisi);

                    var gunYeniFormat =
                        gun.LikidKabuksuzZar.HasValue ||
                        gun.ZarliYumurtaKg.HasValue ||
                        gun.MobaKirikDegeri.HasValue ||
                        gun.MobaKirliDegeri.HasValue ||
                        !string.IsNullOrWhiteSpace(gun.KumesPersoneli) ||
                        !string.IsNullOrWhiteSpace(gun.YeminRasyonNo);

                    gunler.Add(new VGYumurtlamaGenelVeriGunViewModel
                    {
                        HaftaninGunu = haftaninGunu,
                        Veriler = new VGYumurtlamaGenelVeriVerilerViewModel
                        {
                            Gun = gun.Gun,
                            Hafta = gun.Hafta,
                            Tarih = gun.Tarih.ToString("dd.MM.yyyy"),
                            Kriter = kriter,
                            KabukDirenci = gun.KabukDirenci,
                            GizliCatlak = gun.GizliCatlak,
                            HaftaDegeri = haftaDegeri,
                            GunlukOlum = gun.GunlukOlum,
                            OlumYuzde = gun.OlumOrani,
                            TavukSayisi = gun.TavukSayisi,
                            YumurtaSaglam = gun.SaglamYumurta,
                            YumurtaSaglamYuzde = gun.SaglamOrani,
                            YumurtaKirik = gun.KirikYumurta,
                            YumurtaKirikYuzde = gun.KirikOrani,
                            YumurtaKirli = gun.KirliYumurta,
                            YumurtaKirliYuzde = gun.KirliOrani,
                            YumurtaKucukKirik = gunYeniFormat ? 0 : gun.KucukKirik,
                            YumurtaKucukKirikYuzde = gunYeniFormat ? 0 : gun.KucukKirikOrani,
                            YumurtaFire = gunYeniFormat ? 0 : gun.Fire,
                            YumurtaFireYuzde = gunYeniFormat ? 0 : gun.FireOrani,
                            YumurtaZarCatlagi = gunYeniFormat ? gun.KucukKirik : 0,
                            YumurtaZarCatlagiYuzde = gunYeniFormat ? gun.KucukKirikOrani : 0,
                            YumurtaLikit = gunYeniFormat ? gun.Fire : 0,
                            YumurtaLikitYuzde = gunYeniFormat ? gun.FireOrani : 0,
                            YumurtaLikitKabuksuz = gun.LikidKabuksuzZar,
                            YumurtaLikitKabuksuzYuzde = gun.LikidKabuksuzZarOrani,
                            ZarliYumurtaKg = gun.ZarliYumurtaKg,
                            YumurtaToplam = gun.ToplamYumurta,
                            HdYuzde = gun.HdRandiman,
                            HhYuzde = gun.HhRandiman,
                            HaftalikYumurtaTavukBasina = yumurtaBasina,
                            TavukBasinaYumurtaToplami = yumurtaBasinaToplamKumulatif,
                            EggGr = gun.YumurtaGramaji,
                            GunlukYumurtaKutlesi = gun.GunlukYumurtaKutlesi,
                            YemKg = gun.YemKg,
                            HayvanBasiYemGr = gun.HayvanBasiYemGr,
                            SuLt = gun.SuLitre,
                            SuYem = gun.SuYemOrani,
                            YumurtaBasiYem = gun.YumurtaBasiYemGr,
                            Hu = gun.Hu,
                            Uygulamalar = gun.Uygulama ?? string.Empty,
                            KumesPersoneli = gun.KumesPersoneli ?? string.Empty,
                            YemRasyonNo = gun.YeminRasyonNo ?? string.Empty,
                            MobaKirikDegeri = gun.MobaKirikDegeri,
                            MobaKirliDegeri = gun.MobaKirliDegeri,
                        },
                    });
                }

                // Haftalık "Toplam" satırı için biçim, o HAFTANIN kendi
                // toplamlarına bakılarak belirlenir (gunYeniFormat'la aynı
                // mantık, haftalık alanlar üzerinden) — KumesPersoneli/
                // YeminRasyonNo günlük serbest metin alanları olduğu için
                // haftalık agregede karşılığı yok, dört sayısal alan yeterli.
                var haftaYeniFormat =
                    haftalik.ToplamLikidKabuksuzZar.HasValue ||
                    haftalik.ToplamZarliYumurtaKg.HasValue ||
                    haftalik.OrtalamaMobaKirikDegeri.HasValue ||
                    haftalik.OrtalamaMobaKirliDegeri.HasValue;

                gunler.Add(new VGYumurtlamaGenelVeriGunViewModel
                {
                    HaftaninGunu = gunler.Count + 1,
                    Veriler = new VGYumurtlamaGenelVeriVerilerViewModel
                    {
                        Gun = null,
                        Hafta = haftalik.Hafta,
                        Tarih = null,
                        Kriter = "ORT. MKVMT",
                        KabukDirenci = haftalik.OrtalamaKabukDirenci,
                        GizliCatlak = haftalik.OrtalamaGizliCatlak,
                        HaftaDegeri = "Toplam",
                        GunlukOlum = haftalik.ToplamOlum,
                        OlumYuzde = haftalik.OlumOrani,
                        TavukSayisi = haftalik.DonemSonuTavukSayisi,
                        YumurtaSaglam = haftalik.ToplamSaglam,
                        YumurtaSaglamYuzde = haftalik.SaglamOrani,
                        YumurtaKirik = haftalik.ToplamKirik,
                        YumurtaKirikYuzde = haftalik.KirikOrani,
                        YumurtaKirli = haftalik.ToplamKirli,
                        YumurtaKirliYuzde = haftalik.KirliOrani,
                        YumurtaKucukKirik = haftaYeniFormat ? 0 : haftalik.ToplamKucukKirik,
                        YumurtaKucukKirikYuzde = haftaYeniFormat ? 0 : haftalik.KucukKirikOrani,
                        YumurtaFire = haftaYeniFormat ? 0 : haftalik.ToplamFire,
                        YumurtaFireYuzde = haftaYeniFormat ? 0 : haftalik.FireOrani,
                        YumurtaZarCatlagi = haftaYeniFormat ? haftalik.ToplamKucukKirik : 0,
                        YumurtaZarCatlagiYuzde = haftaYeniFormat ? haftalik.KucukKirikOrani : 0,
                        YumurtaLikit = haftaYeniFormat ? haftalik.ToplamFire : 0,
                        YumurtaLikitYuzde = haftaYeniFormat ? haftalik.FireOrani : 0,
                        YumurtaLikitKabuksuz = haftalik.ToplamLikidKabuksuzZar,
                        YumurtaLikitKabuksuzYuzde = haftalik.LikidKabuksuzZarOrani,
                        ZarliYumurtaKg = haftalik.ToplamZarliYumurtaKg,
                        YumurtaToplam = haftalik.ToplamYumurta,
                        HdYuzde = haftalik.HdRandiman,
                        HhYuzde = haftalik.HhRandiman,
                        HaftalikYumurtaTavukBasina = yumurtaBasina,
                        TavukBasinaYumurtaToplami = yumurtaBasinaToplamKumulatif,
                        EggGr = haftalik.OrtalamaYumurtaGramaji,
                        GunlukYumurtaKutlesi = haftalik.YumurtaKutlesi,
                        YemKg = haftalik.ToplamYemKg,
                        HayvanBasiYemGr = haftalik.OrtalamaHayvanBasiYemGr,
                        SuLt = haftalik.ToplamSuLitre,
                        SuYem = haftalik.SuYemOrani,
                        YumurtaBasiYem = haftalik.YumurtaBasiYemGr,
                        Hu = null,
                        Uygulamalar = string.Empty,
                        KumesPersoneli = string.Empty,
                        YemRasyonNo = string.Empty,
                        MobaKirikDegeri = haftalik.OrtalamaMobaKirikDegeri,
                        MobaKirliDegeri = haftalik.OrtalamaMobaKirliDegeri,
                        SatirTipi = "toplam",
                    },
                });

                result.Add(new VGYumurtlamaGenelVeriHaftaViewModel
                {
                    Hafta = grup.Key,
                    Gunler = gunler,
                });
            }

            return result;
        }

        private static decimal? YumurtaBasinaHesapla(VGYumurtlamaHaftalikViewModel haftalik, decimal referansTavukSayisi)
        {
            if (haftalik.ToplamYumurta <= 0 || referansTavukSayisi <= 0)
                return null;

            return haftalik.ToplamYumurta / referansTavukSayisi;
        }

        /// <summary>
        /// Gerçek ana excel kartındaki "KRİTER" sütunu rotasyonu —
        /// Pazartesi/Salı boş, Çarşamba'dan Pazar'a sırasıyla Randıman(%)/
        /// Yem (gr)/Toplam Yem/Adet/Yaşama oranı(%) (bkz. T1 127.PARTİ
        /// SÜPER NİCK#M2.xls üzerinde doğrulanan desen).
        /// </summary>
        private static (string Kriter, object? HaftaDegeri) KriterVeHaftaDegeri(
            int haftaninGunu,
            VGYumurtlamaHaftalikViewModel haftalik,
            decimal baslangicHayvanSayisi,
            decimal ortalamaTavukSayisi)
        {
            switch (haftaninGunu)
            {
                case 3:
                    return ("Randıman(%)", haftalik.HdRandiman);

                case 4:
                    // Gerçek ana excel formülü: ((AB63/AVERAGE(J56:J62))/7)*1000
                    // — yani ToplamYem / haftanın ORTALAMA tavuk sayısı / 7 *
                    // 1000. OrtalamaHayvanBasiYemGr (günlük oranların kendi
                    // ortalaması, AVERAGEIF) BENZER ama matematiksel olarak
                    // FARKLI bir değer — bu yüzden burada ayrıca hesaplanıyor.
                    var yemGr = ortalamaTavukSayisi > 0
                        ? haftalik.ToplamYemKg / ortalamaTavukSayisi / 7m * 1000m
                        : 0m;

                    return ("Yem (gr)", yemGr);

                case 5:
                    return ("Toplam Yem", haftalik.ToplamYemKg);

                case 6:
                    return ("Adet", haftalik.DonemSonuTavukSayisi);

                case 7:
                    var yasamaOrani = baslangicHayvanSayisi > 0
                        ? haftalik.DonemSonuTavukSayisi / baslangicHayvanSayisi * 100m
                        : 0m;

                    return ("Yaşama oranı(%)", yasamaOrani);

                default:
                    return (string.Empty, null);
            }
        }
    }
}
