using KumesScada.Application.Interfaces;
using KumesScada.Application.ViewModels;

namespace KumesScada.Application.Services
{
    /// <summary>
    /// Yüklenen excel dosyalarından tüm kümesler için HAM veri önizlemesi
    /// üretir. Bu uç kümes başına "Kümese Giriş Tarihi" / "Başlangıç
    /// Hayvan Sayısı" gibi bilgileri toplamadığı için Tavuk Sayısı,
    /// Hafta, HD%/HH% gibi hesaplanmış randıman alanlarını ÜRETMEZ.
    /// Tek bir kümes için tam hesaplanmış Yumurtlama Kartı gerekiyorsa
    /// <see cref="IVGYumurtlamaExcelGenerator"/> (api/vg/yumurtlama)
    /// kullanılmalıdır.
    /// </summary>
    public sealed class VGYumurtlamaReportService
        : IVGYumurtlamaReportService
    {
        private readonly IVGYumurtlamaExcelSourceService _sourceService;

        public VGYumurtlamaReportService(
            IVGYumurtlamaExcelSourceService sourceService)
        {
            _sourceService = sourceService;
        }

        public async Task<VGYumurtlamaAllReportViewModel> BuildAsync(
            Stream yemHesaplariStream,
            string yemHesaplariFileName,
            Stream? tasnifStream,
            string? tasnifFileName,
            int year,
            CancellationToken cancellationToken = default)
        {
            var source = await _sourceService.ReadAsync(
                yemHesaplariStream,
                yemHesaplariFileName,
                tasnifStream,
                tasnifFileName,
                year,
                cancellationToken);

            var result = new VGYumurtlamaAllReportViewModel
            {
                FileName = yemHesaplariFileName,
                Year = year
            };

            var groups = source.Data
                .Where(x =>
                    x.Tarih.Year == year &&
                    !string.IsNullOrWhiteSpace(x.KumesNo))
                .GroupBy(x => x.KumesNo.Trim())
                .OrderBy(x => x.Key);

            foreach (var group in groups)
            {
                var kumesResult =
                    new VGYumurtlamaKumesReportViewModel
                    {
                        KumesNo = group.Key
                    };

                foreach (var item in group.OrderBy(x => x.Tarih))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var saglam = item.SaglamYumurta;
                    var kirik = item.KirikYumurta;
                    var kirli = item.KirliYumurta;
                    var fire = item.Fire;

                    /*
                     * Yumurtlama kartındaki "Toplam" formülü:
                     * Sağlam + Kırık + Kirli + Fire
                     * (Küçük Kırık dahil değildir.)
                     * Kırılım verisi hiç yoksa (Rahmiye "Veri Giriş
                     * Tablosu" gibi yalnızca ham adet tutan kaynaklarda)
                     * ham YumurtaAdeti'ni olduğu gibi gösteriyoruz.
                     */
                    decimal? toplamYumurta;

                    if (saglam.HasValue ||
                        kirik.HasValue ||
                        kirli.HasValue ||
                        fire.HasValue)
                    {
                        toplamYumurta =
                            (saglam ?? 0) +
                            (kirik ?? 0) +
                            (kirli ?? 0) +
                            (fire ?? 0);
                    }
                    else
                    {
                        toplamYumurta = item.YumurtaAdeti;
                    }

                    var row =
                        new VGYumurtlamaReportRowViewModel
                        {
                            Tarih = item.Tarih.Date,
                            KumesNo = item.KumesNo,

                            GunlukOlum = item.Olum,

                            GizliCatlak = item.GizliCatlak,

                            SaglamYumurta = saglam,
                            KirikYumurta = kirik,
                            KirliYumurta = kirli,
                            KucukKirik = item.KucukKirik,
                            Fire = fire,

                            ToplamYumurta = toplamYumurta,

                            YumurtaGramaji =
                                item.YumurtaGramaji,

                            YemKg =
                                item.YemTuketim,

                            HayvanBasiYemGr =
                                item.YemTuketimHayvanBasi,

                            SuLitre =
                                item.SuTuketim,

                            CanliAgirlik =
                                item.CanliAgirlik,

                            TekSiraRandiman =
                                item.Randiman,

                            VbapRandiman =
                                item.VbapRandiman,

                            Uygulama =
                                item.Uygulama
                        };


                    // Günlük yumurta kütlesi (HD randıman bu uçta
                    // hesaplanmadığından, gramaj * adet / 1000 olarak
                    // veriliyor.)
                    if (row.ToplamYumurta.HasValue &&
                        row.YumurtaGramaji.HasValue)
                    {
                        row.GunlukYumurtaKutlesi =
                            row.ToplamYumurta.Value *
                            row.YumurtaGramaji.Value /
                            1000m;
                    }


                    // Su / Yem
                    if (row.SuLitre.HasValue &&
                        row.YemKg.HasValue &&
                        row.YemKg.Value != 0)
                    {
                        row.SuYemOrani =
                            row.SuLitre.Value /
                            row.YemKg.Value;
                    }


                    // Yumurta başı yem
                    if (row.YemKg.HasValue &&
                        row.ToplamYumurta.HasValue &&
                        row.ToplamYumurta.Value != 0)
                    {
                        row.YumurtaBasiYemGr =
                            row.YemKg.Value /
                            row.ToplamYumurta.Value *
                            1000m;
                    }


                    kumesResult.Data.Add(row);
                }

                kumesResult.RecordCount =
                    kumesResult.Data.Count;

                result.Kumesler.Add(kumesResult);
            }

            result.KumesCount =
                result.Kumesler.Count;

            return result;
        }
    }
}
