using ClosedXML.Excel;
using KumesScada.Application.Interfaces;
using KumesScada.Domain.Models.Excel;

namespace KumesScada.Infrastructure.Excel
{
    /// <summary>
    /// "Günlük Yem Hesapları" dosyasını okur (Ölü / Yem / Su / Kalite
    /// sayfaları -> satır=tarih, sütun=kümes matrisi).
    ///
    /// Yumurta adedi, yumurta gramajı, kırık/kirli/küçük kırık/fire
    /// kırılımı bu dosyada YOK; onlar "Kümes Tasnif Sayımları"
    /// dosyasından (bkz. <see cref="VGKumesTasnifExcelReader"/>) geliyor.
    /// Canlı ağırlık, kabuk direnci, HU ve tek sıra/VBAP randıman için
    /// de şu an hiçbir kaynak excel'de veri yok; bunlar "Veri Girişleri"
    /// sekmesinden manuel girilmeli.
    /// </summary>
    public class VGExcelReader : IVGExcelReader
    {
        private const int HeaderRow = 2;

        private const int FirstDataRow = 3;

        // C
        private const int FirstKumesColumn = 3;

        // AG
        private const int LastKumesColumn = 33;

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

            ReadDecimalSheet(
                workbook,
                "Ölü",
                dictionary,
                result,
                year,
                (data, value) => data.Olum = value);

            ReadDecimalSheet(
                workbook,
                "Yem",
                dictionary,
                result,
                year,
                (data, value) => data.YemTuketim = value);

            ReadDecimalSheet(
                workbook,
                "Su",
                dictionary,
                result,
                year,
                (data, value) => data.SuTuketim = value);

            ReadDecimalSheet(
                workbook,
                "Kalite",
                dictionary,
                result,
                year,
                (data, value) => data.GizliCatlak = value);

            result.Data = dictionary.Values
                .Where(x =>
                    x.Olum.HasValue ||
                    x.YemTuketim.HasValue ||
                    x.SuTuketim.HasValue ||
                    x.GizliCatlak.HasValue)
                .OrderBy(x => x.Tarih)
                .ThenBy(x => x.KumesNo)
                .ToList();

            return Task.FromResult(result);
        }

        private static void ReadDecimalSheet(
            XLWorkbook workbook,
            string sheetName,
            Dictionary<(DateTime Tarih, string KumesNo),
                VGExcelKumesGunlukData> dictionary,
            VGExcelImportResult importResult,
            int? year,
            Action<VGExcelKumesGunlukData, decimal> setter)
        {
            if (!workbook.TryGetWorksheet(sheetName, out var worksheet))
            {
                importResult.Warnings.Add(
                    $"{sheetName} sayfası bulunamadı.");

                return;
            }

            DateTime? lastDate = null;

            var lastUsedRow =
                worksheet.LastRowUsed()?.RowNumber() ?? FirstDataRow;

            for (var row = FirstDataRow;
                 row <= lastUsedRow;
                 row++)
            {
                var tarih = GetRowDate(
                    worksheet,
                    row,
                    lastDate);

                if (!tarih.HasValue)
                    continue;

                lastDate = tarih;

                if (year.HasValue &&
                    tarih.Value.Year != year.Value)
                {
                    continue;
                }

                for (var column = FirstKumesColumn;
                     column <= LastKumesColumn;
                     column++)
                {
                    var kumesNo = worksheet
                        .Cell(HeaderRow, column)
                        .GetString()
                        .Trim();

                    if (string.IsNullOrWhiteSpace(kumesNo))
                        continue;

                    // "Ölü" sayfasında "Top. Ölü"/"Top. Hayvan", "Yem"/"Su"
                    // sayfalarında "TOPLAM" gibi ara toplam sütunları kümes
                    // kodu değildir — aksi halde bunlar "Kumesler" tablosunda
                    // bulunamadığı için yanıltıcı bir uyarıya yol açar (bkz.
                    // VGKumesTasnifExcelReader'daki aynı kural).
                    if (VGExcelReaderHelpers.Normalize(kumesNo).StartsWith("TOP", StringComparison.Ordinal))
                        continue;

                    var cell = worksheet.Cell(row, column);

                    if (cell.IsEmpty())
                        continue;

                    if (!VGExcelReaderHelpers.TryGetDecimal(cell, out var value))
                        continue;

                    var data = VGExcelReaderHelpers.GetOrCreate(
                        dictionary,
                        tarih.Value.Date,
                        kumesNo);

                    setter(data, value);
                }
            }
        }

        private static DateTime? GetRowDate(
            IXLWorksheet worksheet,
            int row,
            DateTime? previousDate)
        {
            var cell = worksheet.Cell(row, 2);

            if (cell.TryGetValue<DateTime>(out var date))
            {
                return date.Date;
            }

            /*
             * Excel dosyasında tarihlerin bir kısmı
             * formül şeklinde:
             *
             * B4 = B3 + 1
             * B5 = B4 + 1
             *
             * ClosedXML formülü çözemediği durumda
             * önceki tarihten bir gün ilerliyoruz.
             */

            if (cell.HasFormula && previousDate.HasValue)
            {
                return previousDate.Value.AddDays(1).Date;
            }

            return null;
        }
    }
}
