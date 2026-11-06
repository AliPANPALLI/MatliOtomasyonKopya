using System.Globalization;
using ClosedXML.Excel;
using KumesScada.Application.Interfaces;
using KumesScada.Domain.Models.Excel;

namespace KumesScada.Infrastructure.Excel
{
    /// <summary>
    /// "Kümes Tasnif Sayımları" dosyasını okur.
    ///
    /// Bu dosyada her gün ayrı bir sayfadır (sayfa adı "gg.aa.yy",
    /// örn. "01.09.26"). "Genel Toplam", "Stok Sayımı ..." gibi
    /// tarih olmayan sayfalar atlanır.
    ///
    /// Sayfa içinde satırlar sabit etiketli metrikler (B sütunu),
    /// sütunlar ise kümeslerdir (5. satır: Tasnif Makinesi/MOBA grubu,
    /// 8. satır: Kümes No). "MOBA 1 TOPLAM" / "MOBA 2 TOPLAM" gibi
    /// ara toplam sütunları ve boş sütunlar atlanır.
    ///
    /// Eşleme (excel'deki gerçek satır etiketleri üzerinden
    /// doğrulandı — bkz. proje notları):
    ///   "Toplam Yumurta"        -> YumurtaAdeti
    ///   "Yum.Ort.Ağ.(gr)"       -> YumurtaGramaji
    ///   1. "Ara Toplam"         -> SaglamYumurta
    ///        (Klavuz+Small+Medium+Large+Jumbo+S.Jumbo+Çift Sarı toplamı)
    ///   "200.409 Kırık"         -> KirikYumurta
    ///   "200.408 Kirli"         -> KirliYumurta
    ///   "200409 Zar Çatlağı"    -> KucukKirik
    ///        (Yumurtlama kartındaki "Küçük Kırık" sütununa en yakın
    ///        karşılık; dosyada ayrı bir "küçük kırık" satırı yok.)
    ///   "200.693 Likid"         -> Fire
    ///
    /// NOT: "Toplam Yumurta" satırı kaynak dosyada TÜM kırılımların
    /// (küçük kırık dahil) toplamıdır. Yumurtlama kartındaki "Toplam"
    /// formülü ise küçük kırığı hariç tutar (Sağlam+Kırık+Kirli+Fire).
    /// Bu yüzden nihai toplam VGYumurtlamaService.Calculate
    /// içinde kırılımlardan yeniden hesaplanır; buradaki YumurtaAdeti
    /// yalnızca ham/bilgi amaçlı referans değeridir.
    /// </summary>
    public class VGKumesTasnifExcelReader : IVGKumesTasnifExcelReader
    {
        // Sayfa başına etiket satırlarının aranacağı üst sınır.
        private const int LabelScanMaxRow = 60;

        private const int FirstKumesColumn = 3;

        private const int MaxKumesColumn = 120;

        private static readonly string[] DateFormats =
        {
            "dd.MM.yy",
            "d.MM.yy",
            "dd.M.yy",
            "d.M.yy",
            "dd.MM.yyyy",
            "d.MM.yyyy"
        };

        public Task<VGExcelImportResult> ReadAsync(
            Stream stream,
            string fileName,
            int? year = null,
            CancellationToken cancellationToken = default)
        {
            var result = new VGExcelImportResult
            {
                FileName = fileName,
                ImportDate = DateTime.Now
            };

            using var workbook = new XLWorkbook(stream);

            var dictionary =
                new Dictionary<(DateTime Tarih, string KumesNo),
                    VGExcelKumesGunlukData>();

            foreach (var worksheet in workbook.Worksheets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!TryParseSheetDate(worksheet.Name, out var tarih))
                    continue;

                if (year.HasValue && tarih.Year != year.Value)
                    continue;

                ReadDaySheet(
                    worksheet,
                    tarih,
                    dictionary,
                    result);
            }

            result.Data = dictionary.Values
                .Where(x =>
                    x.YumurtaAdeti.HasValue ||
                    x.YumurtaGramaji.HasValue ||
                    x.SaglamYumurta.HasValue ||
                    x.KirikYumurta.HasValue ||
                    x.KirliYumurta.HasValue ||
                    x.KucukKirik.HasValue ||
                    x.Fire.HasValue ||
                    x.Klavuz.HasValue ||
                    x.Small.HasValue ||
                    x.Medium.HasValue ||
                    x.Large.HasValue ||
                    x.Jumbo.HasValue ||
                    x.SJumbo.HasValue ||
                    x.CiftSari.HasValue ||
                    x.HayvanYasiHafta.HasValue ||
                    x.CekimSuresiDk.HasValue ||
                    x.SaatteCekilenYumurtaAdet.HasValue ||
                    x.FiiliHayvanSayisi.HasValue ||
                    x.FiiliYumurtlayanHayvanSayisi.HasValue)
                .OrderBy(x => x.Tarih)
                .ThenBy(x => x.KumesNo)
                .ToList();

            return Task.FromResult(result);
        }

        private static void ReadDaySheet(
            IXLWorksheet worksheet,
            DateTime tarih,
            Dictionary<(DateTime Tarih, string KumesNo),
                VGExcelKumesGunlukData> dictionary,
            VGExcelImportResult importResult)
        {
            int? kumesNoRow = null;
            int? toplamYumurtaRow = null;
            int? gramajRow = null;
            int? araToplamRow = null;
            int? kirikRow = null;
            int? kirliRow = null;
            int? zarCatlagiRow = null;
            int? likidRow = null;
            int? klavuzRow = null;
            int? smallRow = null;
            int? mediumRow = null;
            int? largeRow = null;
            int? jumboRow = null;
            int? sJumboRow = null;
            int? ciftSariRow = null;
            int? hayvanYasiRow = null;
            int? cekimSuresiRow = null;
            int? saatteCekilenRow = null;
            int? fiiliYumurtlayanHayvanRow = null;
            int? fiiliHayvanRow = null;

            var lastLabelRow = Math.Min(
                LabelScanMaxRow,
                worksheet.LastRowUsed()?.RowNumber() ?? LabelScanMaxRow);

            // Yumurta boyut sınıflandırması satırları, excel'de iki kez
            // geçiyor: önce adet (1. blok), sonra "Kümes Yumurta Dağılım
            // Oranları (%)" başlığından sonra yüzde olarak (2. blok). Sadece
            // İLK (adet) eşleşmeyi almak için bu etiketler de "ilk bulunan
            // kazanır" kuralına (== null kontrolü) bırakılıyor; yüzde bloğu
            // ayrıca toplamdan hesaplandığı için adet bloğu yeterli.
            for (var row = 1; row <= lastLabelRow; row++)
            {
                var label = VGExcelReaderHelpers.Normalize(
                    worksheet.Cell(row, 2).GetString());

                if (label.Length == 0)
                    continue;

                if (kumesNoRow == null && label == "KUMESNO")
                    kumesNoRow = row;
                else if (toplamYumurtaRow == null && label == "TOPLAMYUMURTA")
                    toplamYumurtaRow = row;
                else if (gramajRow == null && label.Contains("YUMORTAG"))
                    gramajRow = row;
                else if (kirliRow == null && label.Contains("KIRLI"))
                    kirliRow = row;
                else if (kirikRow == null &&
                         label.Contains("KIRIK") &&
                         !label.Contains("ZARCATLAGI"))
                    kirikRow = row;
                else if (zarCatlagiRow == null && label.Contains("ZARCATLAGI"))
                    zarCatlagiRow = row;
                else if (likidRow == null && label.Contains("LIKID"))
                    likidRow = row;
                else if (araToplamRow == null && label == "ARATOPLAM")
                    araToplamRow = row;
                else if (klavuzRow == null && label.Contains("KLAVUZ"))
                    klavuzRow = row;
                else if (smallRow == null && label.Contains("SMALL"))
                    smallRow = row;
                else if (mediumRow == null && label.Contains("MEDIUM"))
                    mediumRow = row;
                else if (largeRow == null && label.Contains("LARGE"))
                    largeRow = row;
                else if (jumboRow == null &&
                         label.Contains("JUMBO") &&
                         !label.Contains("SJUMBO"))
                    jumboRow = row;
                else if (sJumboRow == null && label.Contains("SJUMBO"))
                    sJumboRow = row;
                else if (ciftSariRow == null && label.Contains("CIFTSARI"))
                    ciftSariRow = row;
                else if (hayvanYasiRow == null && label.Contains("HAYVANYAS"))
                    hayvanYasiRow = row;
                else if (cekimSuresiRow == null && label.Contains("CEKIMSURE"))
                    cekimSuresiRow = row;
                else if (saatteCekilenRow == null && label.Contains("SAATECEKILEN"))
                    saatteCekilenRow = row;
                else if (fiiliYumurtlayanHayvanRow == null &&
                         label.Contains("FIILI") &&
                         label.Contains("YUMURTLAYAN"))
                    fiiliYumurtlayanHayvanRow = row;
                else if (fiiliHayvanRow == null &&
                         label.Contains("FIILI") &&
                         label.Contains("HAYVANSAYISI"))
                    fiiliHayvanRow = row;
            }

            if (kumesNoRow == null)
            {
                importResult.Warnings.Add(
                    $"{worksheet.Name} sayfasında 'Kümes No' satırı bulunamadı, sayfa atlandı.");

                return;
            }

            var lastColumn = Math.Min(
                MaxKumesColumn,
                Math.Max(
                    FirstKumesColumn,
                    worksheet.LastColumnUsed()?.ColumnNumber() ?? FirstKumesColumn));

            for (var column = FirstKumesColumn; column <= lastColumn; column++)
            {
                var kumesNo = worksheet
                    .Cell(kumesNoRow.Value, column)
                    .GetString()
                    .Trim();

                if (string.IsNullOrWhiteSpace(kumesNo))
                    continue;

                if (VGExcelReaderHelpers.Normalize(kumesNo).Contains("TOPLAM"))
                    continue;

                var data = VGExcelReaderHelpers.GetOrCreate(
                    dictionary,
                    tarih,
                    kumesNo);

                SetIfPresent(worksheet, toplamYumurtaRow, column, v => data.YumurtaAdeti = v);
                SetIfPresent(worksheet, gramajRow, column, v => data.YumurtaGramaji = v);
                SetIfPresent(worksheet, araToplamRow, column, v => data.SaglamYumurta = v);
                SetIfPresent(worksheet, kirikRow, column, v => data.KirikYumurta = v);
                SetIfPresent(worksheet, kirliRow, column, v => data.KirliYumurta = v);
                SetIfPresent(worksheet, zarCatlagiRow, column, v => data.KucukKirik = v);
                SetIfPresent(worksheet, likidRow, column, v => data.Fire = v);
                SetIfPresent(worksheet, klavuzRow, column, v => data.Klavuz = v);
                SetIfPresent(worksheet, smallRow, column, v => data.Small = v);
                SetIfPresent(worksheet, mediumRow, column, v => data.Medium = v);
                SetIfPresent(worksheet, largeRow, column, v => data.Large = v);
                SetIfPresent(worksheet, jumboRow, column, v => data.Jumbo = v);
                SetIfPresent(worksheet, sJumboRow, column, v => data.SJumbo = v);
                SetIfPresent(worksheet, ciftSariRow, column, v => data.CiftSari = v);
                SetIfPresent(worksheet, hayvanYasiRow, column, v => data.HayvanYasiHafta = v);
                SetIfPresent(worksheet, cekimSuresiRow, column, v => data.CekimSuresiDk = v);
                SetIfPresent(worksheet, saatteCekilenRow, column, v => data.SaatteCekilenYumurtaAdet = v);
                SetIfPresent(worksheet, fiiliHayvanRow, column, v => data.FiiliHayvanSayisi = v);
                SetIfPresent(worksheet, fiiliYumurtlayanHayvanRow, column, v => data.FiiliYumurtlayanHayvanSayisi = v);
            }
        }

        private static void SetIfPresent(
            IXLWorksheet worksheet,
            int? row,
            int column,
            Action<decimal> setter)
        {
            if (row == null)
                return;

            var cell = worksheet.Cell(row.Value, column);

            if (!VGExcelReaderHelpers.TryGetDecimal(cell, out var value))
                return;

            setter(value);
        }

        private static bool TryParseSheetDate(
            string sheetName,
            out DateTime date)
        {
            return DateTime.TryParseExact(
                sheetName.Trim(),
                DateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);
        }
    }
}
