using KumesScada.Application.Interfaces;
using KumesScada.Application.RequestModels;
using KumesScada.Application.ViewModels;

namespace KumesScada.Application.Services
{
    public class VGYumurtlamaService : IVGYumurtlamaService
    {
        public VGYumurtlamaViewModel Calculate(
            VGYumurtlamaRequest request)
        {
            var result = new VGYumurtlamaViewModel
            {
                KumesNo = request.KumesNo,
                Irk = request.Irk,
                KumeseGirisTarihi = request.KumeseGirisTarihi,
                BaslangicHayvanSayisi = request.BaslangicHayvanSayisi,
                IcSiparisNo = request.IcSiparisNo
            };

            var kaynakVeriler = request.Veriler
                .OrderBy(x => x.Tarih)
                .ToList();

            decimal oncekiTavukSayisi =
                request.BaslangicHayvanSayisi;

            /*
             * Gerçek ana excel kartında "GÜN" sütunu kümese giriş
             * tarihinden DEĞİL, hayvanın yaşından başlar (ör. T1
             * 127.PARTİ'de giriş günü GÜN=1 değil, GÜN=103'tür —
             * hayvanlar kümese zaten ~103 günlükken giriyor). Bu bilgi
             * belirtilmemişse (GirisYasiGun null) geriye dönük uyumluluk
             * için GÜN, giriş tarihinde 1'den başlayan basit bir sayaç
             * olarak hesaplanır.
             */
            var girisYasiGun =
                request.GirisYasiGun ?? 1;

            foreach (var kaynak in kaynakVeriler)
            {
                var olum = kaynak.GunlukOlum ?? 0;

                // Excel J:
                // İlk gün N5-H
                // Sonraki gün J(previous)-H
                var tavukSayisi =
                    Math.Max(0, oncekiTavukSayisi - olum);


                var saglam = kaynak.SaglamYumurta ?? 0;
                var kirik = kaynak.KirikYumurta ?? 0;
                var kirli = kaynak.KirliYumurta ?? 0;
                var kucukKirik = kaynak.KucukKirik ?? 0;
                var fire = kaynak.Fire ?? 0;
                var likidKabuksuzZar = kaynak.LikidKabuksuzZar ?? 0;

                /*
                 * Excel (eski kart, ör. "T1 127.PARTİ"):
                 *
                 * U = K + M + O + Q + S
                 *
                 * (gerçek ana excel kartının "A" sayfasındaki TOPLAM
                 * satırının ham değerleri üzerinden doğrulandı — ör.
                 * 31448+1970+3503+60+865=37846, dosyadaki TOPLAM hücresiyle
                 * birebir eşleşiyor. Küçük kırık (Q) toplam formülüne
                 * DAHİLDİR — önceki not yanlıştı.)
                 *
                 * Yeni kart biçiminde (ör. "T14 135.PARTİ") TOPLAM sütunu
                 * "Likid - Kabuksuz Zar"yı da içine alacak şekilde
                 * GENİŞLEMİŞ — doğrulandı (ör. satır106:
                 * 107638+330+2954+714+116+720=112472, kartın TOPLAM
                 * hücresiyle birebir eşleşiyor; bu alan hariç tutulursa
                 * HD%/HH% ve tüm yüzde sütunları excel'den düşük çıkıyordu
                 * — bkz. proje notları). Eski kartlarda bu alan hep null
                 * (?? 0) olduğu için formül geriye dönük aynı sonucu verir.
                 */
                var toplamYumurta =
                    saglam +
                    kirik +
                    kucukKirik +
                    kirli +
                    fire +
                    likidKabuksuzZar;

                /*
                 * Bazı kaynaklarda (ör. Rahmiye "Veri Giriş Tablosu")
                 * sağlam/kırık/kirli/fire kırılımı hiç tutulmuyor,
                 * yalnızca günlük ham yumurta adedi var. Kırılım
                 * verisi yoksa (toplam 0 çıkıyorsa) ham adedi kullan.
                 */
                if (toplamYumurta == 0 &&
                    kaynak.ToplamYumurtaAdedi.HasValue &&
                    kaynak.ToplamYumurtaAdedi.Value > 0)
                {
                    toplamYumurta = kaynak.ToplamYumurtaAdedi.Value;
                }


                var hdRandiman =
                    SafePercent(
                        toplamYumurta,
                        tavukSayisi);


                /*
                 * Excel W:
                 * U / N5
                 */
                var hhRandiman =
                    SafePercent(
                        toplamYumurta,
                        request.BaslangicHayvanSayisi);


                var yemKg =
                    kaynak.YemKg ?? 0;

                var suLitre =
                    kaynak.SuLitre ?? 0;


                /*
                 * Excel AC:
                 * Yem / Tavuk Sayısı * 1000
                 */
                var hayvanBasiYem =
                    SafeDivide(
                        yemKg * 1000,
                        tavukSayisi);


                /*
                 * Excel AE:
                 * Su / Yem
                 */
                var suYem =
                    SafeDivide(
                        suLitre,
                        yemKg);


                /*
                 * Excel AA:
                 * HD% * Egg/gr / 100
                 */
                decimal? yumurtaKutlesi = null;

                if (kaynak.YumurtaGramaji.HasValue)
                {
                    yumurtaKutlesi =
                        hdRandiman *
                        kaynak.YumurtaGramaji.Value /
                        100m;
                }


                /*
                 * Excel'de günlük AF hücresi çoğunlukla boş.
                 * Ama servis tarafında istersen hesaplanabilir.
                 */
                decimal? yumurtaBasiYem = null;

                if (toplamYumurta > 0)
                {
                    yumurtaBasiYem =
                        yemKg /
                        toplamYumurta *
                        1000m;
                }


                var satir = new VGYumurtlamaGunlukViewModel
                {
                    Gun = CalculateGun(
                        request.KumeseGirisTarihi,
                        kaynak.Tarih,
                        girisYasiGun),

                    Hafta = CalculateWeek(
                        request.KumeseGirisTarihi,
                        kaynak.Tarih,
                        girisYasiGun,
                        request.GirisHaftasi),

                    Tarih = kaynak.Tarih.Date,

                    KabukDirenci =
                        kaynak.KabukDirenci,

                    GizliCatlak =
                        kaynak.GizliCatlak,

                    GunlukOlum = olum,

                    /*
                     * Excel'in ilerleyen satırlarında:
                     * H / N5
                     */
                    OlumOrani =
                        SafePercent(
                            olum,
                            request.BaslangicHayvanSayisi),

                    TavukSayisi =
                        tavukSayisi,

                    SaglamYumurta =
                        saglam,

                    /*
                     * DİKKAT: Tavuk sayısına göre DEĞİL — gerçek ana excel
                     * kartında (bkz. "A" sayfası: IFERROR(Q/AA*100,"0"),
                     * AA = o günün Toplam Yumurta hücresi) Sağlam/Kırık/
                     * Kirli/Fire yüzdeleri o günün TOPLAM YUMURTASINA (U)
                     * göre hesaplanıyor — Küçük Kırık oranıyla aynı mantık.
                     */
                    SaglamOrani =
                        SafePercent(
                            saglam,
                            toplamYumurta),

                    KirikYumurta =
                        kirik,

                    KirikOrani =
                        SafePercent(
                            kirik,
                            toplamYumurta),

                    KirliYumurta =
                        kirli,

                    KirliOrani =
                        SafePercent(
                            kirli,
                            toplamYumurta),

                    KucukKirik =
                        kucukKirik,

                    /*
                     * Excel sonraki bölümlerde
                     * Q / U * 100 kullanıyor.
                     */
                    KucukKirikOrani =
                        SafePercent(
                            kucukKirik,
                            toplamYumurta),

                    Fire =
                        fire,

                    FireOrani =
                        SafePercent(
                            fire,
                            toplamYumurta),

                    // "Likid - Kabuksuz Zar" (Fire'ın alt kırılımı) ve
                    // "Zarlı Yumurta (Kg)" toplam yumurta formülüne DAHİL
                    // DEĞİL — sadece bilgi amaçlı, ana excel'de de ayrı
                    // sütunlar (bkz. VGExcelKumesGunlukData).
                    LikidKabuksuzZar =
                        kaynak.LikidKabuksuzZar,

                    LikidKabuksuzZarOrani =
                        SizeOrani(kaynak.LikidKabuksuzZar, toplamYumurta),

                    ZarliYumurtaKg =
                        kaynak.ZarliYumurtaKg,

                    MobaKirikDegeri =
                        kaynak.MobaKirikDegeri,

                    MobaKirliDegeri =
                        kaynak.MobaKirliDegeri,

                    KumesPersoneli =
                        kaynak.KumesPersoneli,

                    YeminRasyonNo =
                        kaynak.YeminRasyonNo,

                    ToplamYumurta =
                        toplamYumurta,

                    HdRandiman =
                        hdRandiman,

                    HhRandiman =
                        hhRandiman,

                    YumurtaGramaji =
                        kaynak.YumurtaGramaji,

                    GunlukYumurtaKutlesi =
                        yumurtaKutlesi,

                    CanliAgirlik =
                        kaynak.CanliAgirlik,

                    YemKg =
                        yemKg,

                    HayvanBasiYemGr =
                        hayvanBasiYem,

                    SuLitre =
                        suLitre,

                    SuYemOrani =
                        suYem,

                    YumurtaBasiYemGr =
                        yumurtaBasiYem,

                    Hu =
                        kaynak.Hu,

                    TekSiraRandiman =
                        kaynak.TekSiraRandiman,

                    VbapRandiman =
                        kaynak.VbapRandiman,

                    Uygulama =
                        kaynak.Uygulama,

                    Klavuz =
                        kaynak.Klavuz,

                    KlavuzOrani =
                        SizeOrani(kaynak.Klavuz, toplamYumurta),

                    Small =
                        kaynak.Small,

                    SmallOrani =
                        SizeOrani(kaynak.Small, toplamYumurta),

                    Medium =
                        kaynak.Medium,

                    MediumOrani =
                        SizeOrani(kaynak.Medium, toplamYumurta),

                    Large =
                        kaynak.Large,

                    LargeOrani =
                        SizeOrani(kaynak.Large, toplamYumurta),

                    Jumbo =
                        kaynak.Jumbo,

                    JumboOrani =
                        SizeOrani(kaynak.Jumbo, toplamYumurta),

                    SJumbo =
                        kaynak.SJumbo,

                    SJumboOrani =
                        SizeOrani(kaynak.SJumbo, toplamYumurta),

                    CiftSari =
                        kaynak.CiftSari,

                    CiftSariOrani =
                        SizeOrani(kaynak.CiftSari, toplamYumurta),

                    HayvanYasiHafta =
                        kaynak.HayvanYasiHafta,

                    CekimSuresiDk =
                        kaynak.CekimSuresiDk,

                    SaatteCekilenYumurtaAdet =
                        kaynak.SaatteCekilenYumurtaAdet,

                    FiiliHayvanSayisi =
                        kaynak.FiiliHayvanSayisi,

                    FiiliYumurtlayanHayvanSayisi =
                        kaynak.FiiliYumurtlayanHayvanSayisi
                };


                result.GunlukVeriler.Add(satir);

                oncekiTavukSayisi =
                    tavukSayisi;
            }


            /*
             * Excel'deki haftalık toplam satırları.
             */
            result.HaftalikVeriler =
                CalculateWeekly(
                    result.GunlukVeriler,
                    request.BaslangicHayvanSayisi);


            return result;
        }


        private static List<VGYumurtlamaHaftalikViewModel>
            CalculateWeekly(
                List<VGYumurtlamaGunlukViewModel> gunlukler,
                decimal baslangicHayvanSayisi)
        {
            var result =
                new List<VGYumurtlamaHaftalikViewModel>();


            var haftalar = gunlukler
                .GroupBy(x => x.Hafta)
                .OrderBy(x => x.Key);


            foreach (var hafta in haftalar)
            {
                var rows = hafta
                    .OrderBy(x => x.Tarih)
                    .ToList();


                if (rows.Count == 0)
                    continue;


                var toplamOlum =
                    rows.Sum(x => x.GunlukOlum);

                var toplamSaglam =
                    rows.Sum(x => x.SaglamYumurta);

                var toplamKirik =
                    rows.Sum(x => x.KirikYumurta);

                var toplamKirli =
                    rows.Sum(x => x.KirliYumurta);

                var toplamKucukKirik =
                    rows.Sum(x => x.KucukKirik);

                var toplamFire =
                    rows.Sum(x => x.Fire);

                var toplamLikidKabuksuzZar =
                    SumNullable(rows.Select(x => x.LikidKabuksuzZar));

                var toplamZarliYumurtaKg =
                    SumNullable(rows.Select(x => x.ZarliYumurtaKg));

                var toplamYumurta =
                    rows.Sum(x => x.ToplamYumurta);

                var toplamYem =
                    rows.Sum(x => x.YemKg);

                var toplamSu =
                    rows.Sum(x => x.SuLitre);

                var toplamKlavuz =
                    SumNullable(rows.Select(x => x.Klavuz));

                var toplamSmall =
                    SumNullable(rows.Select(x => x.Small));

                var toplamMedium =
                    SumNullable(rows.Select(x => x.Medium));

                var toplamLarge =
                    SumNullable(rows.Select(x => x.Large));

                var toplamJumbo =
                    SumNullable(rows.Select(x => x.Jumbo));

                var toplamSJumbo =
                    SumNullable(rows.Select(x => x.SJumbo));

                var toplamCiftSari =
                    SumNullable(rows.Select(x => x.CiftSari));


                /*
                 * Excel:
                 * V haftalık =
                 * SUM(U daily) /
                 * SUM(J daily) * 100
                 *
                 * SU/YEM ve YUMURTA BAŞI YEM gibi diğer oran sütunlarında
                 * kayıt girilmeyen (yumurta=0) günler payda VE paydanın
                 * ikisine de 0 eklediği için kendiliğinden hesaba
                 * katılmıyor. HD%'de ise payda TavukSayisi — kayıt
                 * girilmeyen bir günde bile hayvan sayısı 0 DEĞİL (sürü
                 * o gün de var) — bu yüzden tüm 7 günün TavukSayisi'si
                 * toplanırsa, pay sadece kayıtlı (ör. 5) günün yumurtasını
                 * içerirken payda 7 günün hayvanını içerir ve oran
                 * olduğundan düşük çıkar. Bu yüzden payda da SADECE
                 * yumurta kaydı olan günlerin TavukSayisi'si toplanır.
                 */
                var tavukSayisiYumurtaliGunler =
                    rows
                        .Where(x => x.ToplamYumurta != 0)
                        .Sum(x => x.TavukSayisi);

                var hdRandiman =
                    SafePercent(
                        toplamYumurta,
                        tavukSayisiYumurtaliGunler);


                /*
                 * Excel W haftalık:
                 * U_week / N5 / 7
                 *
                 * Gün sayısı 7 değilse gerçek gün sayısını kullanmak
                 * daha güvenli — AMA "gerçek gün sayısı" rows.Count
                 * (haftadaki TÜM takvim günü satırı, kayıtsız/0 günler
                 * dahil) DEĞİL, HD%'deki aynı sorunun ikizi: haftanın
                 * bazı günlerinde hiç yumurta kaydı yoksa (ör. veri
                 * girişi henüz o güne gelmemiş/boş şablon günü) o
                 * günler de 7'ye dahil edilirse pay (sadece kayıtlı
                 * günlerin yumurtası) sabit kalırken payda şişer ve
                 * oran olduğundan düşük çıkar (bkz. hdRandiman üzerindeki
                 * aynı not). Bu yüzden SADECE yumurta kaydı olan günler
                 * sayılıyor.
                 */
                var yumurtaliGunSayisi =
                    rows.Count(x => x.ToplamYumurta != 0);

                var hhRandiman =
                    yumurtaliGunSayisi == 0
                        ? 0
                        : SafePercent(
                            toplamYumurta,
                            baslangicHayvanSayisi) /
                          yumurtaliGunSayisi;


                var ortalamaGramaj =
                    AverageNullable(
                        rows.Select(x =>
                            x.YumurtaGramaji));


                decimal? yumurtaKutlesi = null;

                if (ortalamaGramaj.HasValue)
                {
                    yumurtaKutlesi =
                        hdRandiman *
                        ortalamaGramaj.Value /
                        100m;
                }


                var sonTavukSayisi =
                    rows.Last().TavukSayisi;


                /*
                 * Excel bazı haftalarda:
                 * AVERAGEIF(AC,"<>0")
                 */
                var hayvanBasiYem =
                    AverageNonZero(
                        rows.Select(x =>
                            x.HayvanBasiYemGr));


                decimal? yumurtaBasiYem = null;

                if (toplamYumurta > 0)
                {
                    yumurtaBasiYem =
                        toplamYem /
                        toplamYumurta *
                        1000m;
                }


                result.Add(
                    new VGYumurtlamaHaftalikViewModel
                    {
                        Hafta =
                            hafta.Key,

                        BaslangicTarihi =
                            rows.First().Tarih,

                        BitisTarihi =
                            rows.Last().Tarih,

                        OrtalamaKabukDirenci =
                            AverageNullable(
                                rows.Select(x =>
                                    x.KabukDirenci)),

                        OrtalamaGizliCatlak =
                            AverageNullable(
                                rows.Select(x =>
                                    x.GizliCatlak)),

                        ToplamOlum =
                            toplamOlum,

                        OlumOrani =
                            SafePercent(
                                toplamOlum,
                                baslangicHayvanSayisi),

                        DonemSonuTavukSayisi =
                            sonTavukSayisi,

                        ToplamSaglam =
                            toplamSaglam,

                        /*
                         * Sheet'in yeni bölümünde haftalık:
                         * K / U * 100
                         */
                        SaglamOrani =
                            SafePercent(
                                toplamSaglam,
                                toplamYumurta),

                        ToplamKirik =
                            toplamKirik,

                        KirikOrani =
                            SafePercent(
                                toplamKirik,
                                toplamYumurta),

                        ToplamKirli =
                            toplamKirli,

                        KirliOrani =
                            SafePercent(
                                toplamKirli,
                                toplamYumurta),

                        ToplamKucukKirik =
                            toplamKucukKirik,

                        KucukKirikOrani =
                            SafePercent(
                                toplamKucukKirik,
                                toplamYumurta),

                        ToplamFire =
                            toplamFire,

                        FireOrani =
                            SafePercent(
                                toplamFire,
                                toplamYumurta),

                        ToplamLikidKabuksuzZar =
                            toplamLikidKabuksuzZar,

                        LikidKabuksuzZarOrani =
                            SizeOrani(toplamLikidKabuksuzZar, toplamYumurta),

                        ToplamZarliYumurtaKg =
                            toplamZarliYumurtaKg,

                        OrtalamaMobaKirikDegeri =
                            AverageNullable(
                                rows.Select(x =>
                                    x.MobaKirikDegeri)),

                        OrtalamaMobaKirliDegeri =
                            AverageNullable(
                                rows.Select(x =>
                                    x.MobaKirliDegeri)),

                        ToplamYumurta =
                            toplamYumurta,

                        HdRandiman =
                            hdRandiman,

                        HhRandiman =
                            hhRandiman,

                        OrtalamaYumurtaGramaji =
                            ortalamaGramaj,

                        YumurtaKutlesi =
                            yumurtaKutlesi,

                        ToplamYemKg =
                            toplamYem,

                        OrtalamaHayvanBasiYemGr =
                            hayvanBasiYem,

                        ToplamSuLitre =
                            toplamSu,

                        SuYemOrani =
                            SafeDivide(
                                toplamSu,
                                toplamYem),

                        YumurtaBasiYemGr =
                            yumurtaBasiYem,

                        OrtalamaTekSiraRandiman =
                            AverageNullable(
                                rows.Select(x =>
                                    x.TekSiraRandiman)),

                        OrtalamaVbapRandiman =
                            AverageNullable(
                                rows.Select(x =>
                                    x.VbapRandiman)),

                        ToplamKlavuz =
                            toplamKlavuz,

                        KlavuzOrani =
                            SizeOrani(toplamKlavuz, toplamYumurta),

                        ToplamSmall =
                            toplamSmall,

                        SmallOrani =
                            SizeOrani(toplamSmall, toplamYumurta),

                        ToplamMedium =
                            toplamMedium,

                        MediumOrani =
                            SizeOrani(toplamMedium, toplamYumurta),

                        ToplamLarge =
                            toplamLarge,

                        LargeOrani =
                            SizeOrani(toplamLarge, toplamYumurta),

                        ToplamJumbo =
                            toplamJumbo,

                        JumboOrani =
                            SizeOrani(toplamJumbo, toplamYumurta),

                        ToplamSJumbo =
                            toplamSJumbo,

                        SJumboOrani =
                            SizeOrani(toplamSJumbo, toplamYumurta),

                        ToplamCiftSari =
                            toplamCiftSari,

                        CiftSariOrani =
                            SizeOrani(toplamCiftSari, toplamYumurta),

                        OrtalamaHayvanYasiHafta =
                            AverageNullable(
                                rows.Select(x =>
                                    x.HayvanYasiHafta)),

                        OrtalamaCekimSuresiDk =
                            AverageNullable(
                                rows.Select(x =>
                                    x.CekimSuresiDk)),

                        OrtalamaSaatteCekilenYumurtaAdet =
                            AverageNullable(
                                rows.Select(x =>
                                    x.SaatteCekilenYumurtaAdet)),

                        OrtalamaFiiliHayvanSayisi =
                            AverageNullable(
                                rows.Select(x =>
                                    x.FiiliHayvanSayisi)),

                        OrtalamaFiiliYumurtlayanHayvanSayisi =
                            AverageNullable(
                                rows.Select(x =>
                                    x.FiiliYumurtlayanHayvanSayisi))
                    });
            }


            return result;
        }


        private static decimal SafeDivide(
            decimal numerator,
            decimal denominator)
        {
            if (denominator == 0)
                return 0;

            return numerator / denominator;
        }


        private static decimal SafePercent(
            decimal numerator,
            decimal denominator)
        {
            if (denominator == 0)
                return 0;

            return numerator /
                   denominator *
                   100m;
        }


        /// <summary>
        /// Yumurta boyut sınıflandırması (Small/Medium/Large/...) için
        /// oran hesabı — kaynak değer yoksa null döner (tasnif dosyası
        /// yüklenmeyen/boş günlerde "0%" yazmamak için).
        /// </summary>
        private static decimal? SizeOrani(
            decimal? deger,
            decimal toplamYumurta)
        {
            if (!deger.HasValue)
                return null;

            return SafePercent(
                deger.Value,
                toplamYumurta);
        }


        private static decimal? SumNullable(
            IEnumerable<decimal?> values)
        {
            var list = values
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();

            if (list.Count == 0)
                return null;

            return list.Sum();
        }


        private static decimal? AverageNullable(
            IEnumerable<decimal?> values)
        {
            var list = values
                .Where(x => x.HasValue && x.Value != 0)
                .Select(x => x!.Value)
                .ToList();

            if (list.Count == 0)
                return null;

            return list.Average();
        }


        private static decimal AverageNonZero(
            IEnumerable<decimal> values)
        {
            var list = values
                .Where(x => x != 0)
                .ToList();

            if (list.Count == 0)
                return 0;

            return list.Average();
        }


        /// <summary>
        /// GÜN = hayvanın giriş günündeki yaşı + aradan geçen gün sayısı
        /// (bkz. request.GirisYasiGun üzerindeki not — gerçek ana
        /// exceldeki GÜN sütunuyla birebir eşleşir).
        /// </summary>
        private static int CalculateGun(
            DateTime girisTarihi,
            DateTime tarih,
            int girisYasiGun)
        {
            var days =
                (tarih.Date -
                 girisTarihi.Date)
                .Days;

            return girisYasiGun + days;
        }

        /// <summary>
        /// HAFTA, GÜN sayacından BAĞIMSIZ, TAKVİM haftasına (Pazartesi
        /// başlangıç — Pazar bitiş) göre değişir — gerçek ana excel
        /// kartında doğrulandı (bkz. T1 127.PARTİ: GÜN 101 Pzt'den GÜN
        /// 107 Pazar'a kadar HAFTA sabit 15, GÜN 108'deki sıradaki
        /// Pazartesi'de HAFTA 16 oluyor). Giriş tarihinin içinde
        /// bulunduğu hafta <paramref name="girisHaftasi"/> ile biliniyorsa
        /// doğrudan O kullanılır — sonraki/önceki haftalar oradan
        /// Pazartesi'den Pazartesi'ye sayılır. Bilinmiyorsa (null; ör.
        /// Rahmiye kaynaklı veya eski kayıtlarda) eskisi gibi
        /// CEILING(girisYasiGun / 7) ile TAHMİN edilir — AMA bu tahmin
        /// hayvanın 1. gününün hangi haftanın hangi gününe denk geldiğine
        /// bağlı olarak karttan karta 1 hafta kayabiliyor (bkz. proje
        /// notları — "T14 135.PARTİ"de GÜN 118 gerçekte HAFTA 18 iken bu
        /// formül 17 verirdi), bu yüzden mümkün olduğunda gerçek
        /// girisHaftasi tercih edilmeli.
        /// </summary>
        private static int CalculateWeek(
            DateTime girisTarihi,
            DateTime tarih,
            int girisYasiGun,
            int? girisHaftasi = null)
        {
            var haftaGiriste =
                girisHaftasi ??
                (int)Math.Ceiling(girisYasiGun / 7m);

            var pazartesiGiris =
                MondayOf(girisTarihi);

            var pazartesiTarih =
                MondayOf(tarih);

            var haftaFarki =
                (pazartesiTarih - pazartesiGiris).Days / 7;

            return haftaGiriste + haftaFarki;
        }

        private static DateTime MondayOf(DateTime tarih)
        {
            // DayOfWeek: Pazar=0, Pazartesi=1, ... Cumartesi=6.
            // Pazartesi'yi haftanın 0. günü yapacak şekilde kaydır.
            var gunIndeksi =
                ((int)tarih.Date.DayOfWeek + 6) % 7;

            return tarih.Date.AddDays(-gunIndeksi);
        }
    }
}