
using ClosedXML.Excel;
using KumesScada.Application.Interfaces;
using KumesScada.Application.ViewModels;

namespace KumesScada.Infrastructure.Services.Excel
{
    public sealed class VGYumurtlamaExcelExportService
        : IVGYumurtlamaExcelExportService
    {
        public byte[] Export(
            VGYumurtlamaAllReportViewModel report)
        {
            using var workbook = new XLWorkbook();

            foreach (var kumes in report.Kumesler)
            {
                // Excel sheet adı max 31 karakter.
                var sheetName = SanitizeSheetName(kumes.KumesNo);

                var ws = workbook.Worksheets.Add(sheetName);

                // =================================================
                // ÜST BİLGİLER
                // =================================================

                ws.Cell("A1").Value = "KÜMES";
                ws.Cell("B1").Value = kumes.KumesNo;

                ws.Cell("D1").Value = "YIL";
                ws.Cell("E1").Value = report.Year;

                ws.Cell("G1").Value = "KAYNAK";
                ws.Cell("H1").Value = report.FileName;


                // =================================================
                // BAŞLIKLAR
                // =================================================

                var headerRow = 3;

                ws.Cell(headerRow, 1).Value = "Tarih";
                ws.Cell(headerRow, 2).Value = "Kümes";

                ws.Cell(headerRow, 3).Value = "Günlük Ölüm";
                ws.Cell(headerRow, 4).Value = "Ölüm %";
                ws.Cell(headerRow, 5).Value = "Tavuk Sayısı";

                ws.Cell(headerRow, 6).Value = "Toplam Yumurta";
                ws.Cell(headerRow, 7).Value = "HD Randıman";

                ws.Cell(headerRow, 8).Value = "Yumurta Gramajı";
                ws.Cell(headerRow, 9).Value = "Günlük Yumurta Kütlesi";

                ws.Cell(headerRow, 10).Value = "Yem Kg";
                ws.Cell(headerRow, 11).Value = "Hayvan Başı Yem Gr";

                ws.Cell(headerRow, 12).Value = "Su Litre";
                ws.Cell(headerRow, 13).Value = "Su / Yem";

                ws.Cell(headerRow, 14).Value = "Yumurta Başı Yem Gr";

                ws.Cell(headerRow, 15).Value = "Canlı Ağırlık";

                ws.Cell(headerRow, 16).Value = "Tek Sıra Randıman";
                ws.Cell(headerRow, 17).Value = "VBAP Randıman";

                ws.Cell(headerRow, 18).Value = "Uygulama";


                // =================================================
                // VERİLER
                // =================================================

                var rowNumber = headerRow + 1;

                foreach (var item in kumes.Data)
                {
                    ws.Cell(rowNumber, 1).Value =
                        item.Tarih;

                    ws.Cell(rowNumber, 2).Value =
                        item.KumesNo;

                    SetDecimal(
                        ws.Cell(rowNumber, 3),
                        item.GunlukOlum);

                    SetDecimal(
                        ws.Cell(rowNumber, 4),
                        item.OlumOrani);

                    SetDecimal(
                        ws.Cell(rowNumber, 5),
                        item.TavukSayisi);

                    SetDecimal(
                        ws.Cell(rowNumber, 6),
                        item.ToplamYumurta);

                    SetDecimal(
                        ws.Cell(rowNumber, 7),
                        item.HdRandiman);

                    SetDecimal(
                        ws.Cell(rowNumber, 8),
                        item.YumurtaGramaji);

                    SetDecimal(
                        ws.Cell(rowNumber, 9),
                        item.GunlukYumurtaKutlesi);

                    SetDecimal(
                        ws.Cell(rowNumber, 10),
                        item.YemKg);

                    SetDecimal(
                        ws.Cell(rowNumber, 11),
                        item.HayvanBasiYemGr);

                    SetDecimal(
                        ws.Cell(rowNumber, 12),
                        item.SuLitre);

                    SetDecimal(
                        ws.Cell(rowNumber, 13),
                        item.SuYemOrani);

                    SetDecimal(
                        ws.Cell(rowNumber, 14),
                        item.YumurtaBasiYemGr);

                    SetDecimal(
                        ws.Cell(rowNumber, 15),
                        item.CanliAgirlik);

                    SetDecimal(
                        ws.Cell(rowNumber, 16),
                        item.TekSiraRandiman);

                    SetDecimal(
                        ws.Cell(rowNumber, 17),
                        item.VbapRandiman);

                    ws.Cell(rowNumber, 18).Value =
                        item.Uygulama ?? string.Empty;

                    rowNumber++;
                }


                // =================================================
                // FORMAT
                // =================================================

                ws.Row(headerRow).Style.Font.Bold = true;

                ws.SheetView.FreezeRows(headerRow);

                ws.Column(1)
                    .Style.DateFormat.Format =
                    "dd.MM.yyyy";

                ws.Columns().AdjustToContents();
            }


            using var memoryStream =
                new MemoryStream();

            workbook.SaveAs(memoryStream);

            return memoryStream.ToArray();
        }


        private static void SetDecimal(
            IXLCell cell,
            decimal? value)
        {
            if (value.HasValue)
                cell.Value = value.Value;
        }


        private static string SanitizeSheetName(
            string name)
        {
            var invalidChars =
                new[] { ':', '\\', '/', '?', '*', '[', ']' };

            foreach (var invalidChar in invalidChars)
            {
                name =
                    name.Replace(
                        invalidChar,
                        '_');
            }

            if (name.Length > 31)
                name = name[..31];

            return name;
        }
    }
}