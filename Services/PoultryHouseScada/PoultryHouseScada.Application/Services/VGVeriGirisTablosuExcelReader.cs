using ClosedXML.Excel;
using PoultryHouseScada.Application.Interfaces;
using PoultryHouseScada.Domain.Models.Excel;

namespace PoultryHouseScada.Infrastructure.Excel
{
    /// <summary>
    /// Rahmiye çiftliği "Veri Giriş Tablosu.xlsx" okuyucusu.
    /// Bkz. <see cref="IVGVeriGirisTablosuExcelReader"/>.
    /// </summary>
    public class VGVeriGirisTablosuExcelReader : IVGVeriGirisTablosuExcelReader
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
                workbook, "Ölü Tablosu", dictionary, result, year,
                (data, value) => data.Olum = value);

            ReadDecimalSheet(
                workbook, "Günlük Yumurta Adeti", dictionary, result, year,
                (data, value) => data.YumurtaAdeti = value);

            ReadDecimalSheet(
                workbook, "Yumurta Gramajı", dictionary, result, year,
                (data, value) => data.YumurtaGramaji = value);

            ReadDecimalSheet(
                workbook, "Canlı Ağırlık", dictionary, result, year,
                (data, value) => data.CanliAgirlik = value);

            ReadDecimalSheet(
                workbook, "Yem Tüketim Tablosu", dictionary, result, year,
                (data, value) => data.YemTuketim = value);

            ReadDecimalSheet(
                workbook, "Yem Tüketim Tablosu(H.B.) GR", dictionary, result, year,
                (data, value) => data.YemTuketimHayvanBasi = value);

            ReadDecimalSheet(
                workbook, "Su Tüketim Tablosu", dictionary, result, year,
                (data, value) => data.SuTuketim = value);

            ReadDecimalSheet(
                workbook, "Tek Sıra Randımanlar", dictionary, result, year,
                (data, value) => data.Randiman = value);

            ReadDecimalSheet(
                workbook, "VBAP RAND.", dictionary, result, year,
                (data, value) => data.VbapRandiman = value);

            ReadTextSheet(
                workbook, "Uygulamalar", dictionary, result, year,
                (data, value) => data.Uygulama = value);

            result.Data = dictionary.Values
                .Where(x =>
                    x.Olum.HasValue ||
                    x.YumurtaAdeti.HasValue ||
                    x.YumurtaGramaji.HasValue ||
                    x.CanliAgirlik.HasValue ||
                    x.YemTuketim.HasValue ||
                    x.YemTuketimHayvanBasi.HasValue ||
                    x.SuTuketim.HasValue ||
                    x.Randiman.HasValue ||
                    x.VbapRandiman.HasValue ||
                    !string.IsNullOrWhiteSpace(x.Uygulama))
                .OrderBy(x => x.Tarih)
                .ThenBy(x => x.KumesNo)
                .ToList();

            return Task.FromResult(result);
        }

        /// <summary>
        /// Bu formatın sayfalarından herhangi biri workbook'ta varsa
        /// true döner; dosyayı Rahmiye formatı olarak tanımak için
        /// kullanılır.
        /// </summary>
        public static bool Matches(XLWorkbook workbook)
        {
            return workbook.TryGetWorksheet("Ölü Tablosu", out _) ||
                   workbook.TryGetWorksheet("Günlük Yumurta Adeti", out _);
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

            for (var row = FirstDataRow; row <= lastUsedRow; row++)
            {
                var tarih = GetRowDate(worksheet, row, lastDate);

                if (!tarih.HasValue)
                    continue;

                lastDate = tarih;

                if (year.HasValue && tarih.Value.Year != year.Value)
                    continue;

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

        private static void ReadTextSheet(
            XLWorkbook workbook,
            string sheetName,
            Dictionary<(DateTime Tarih, string KumesNo),
                VGExcelKumesGunlukData> dictionary,
            VGExcelImportResult importResult,
            int? year,
            Action<VGExcelKumesGunlukData, string> setter)
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

            for (var row = FirstDataRow; row <= lastUsedRow; row++)
            {
                var tarih = GetRowDate(worksheet, row, lastDate);

                if (!tarih.HasValue)
                    continue;

                lastDate = tarih;

                if (year.HasValue && tarih.Value.Year != year.Value)
                    continue;

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

                    var value = worksheet
                        .Cell(row, column)
                        .GetString()
                        .Trim();

                    if (string.IsNullOrWhiteSpace(value))
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
             * Tarihlerin bir kısmı formül şeklinde (B4=B3+1 vb.).
             * ClosedXML formülü çözemezse önceki tarihten bir gün
             * ilerliyoruz.
             */
            if (cell.HasFormula && previousDate.HasValue)
            {
                return previousDate.Value.AddDays(1).Date;
            }

            return null;
        }
    }
}
