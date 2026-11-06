using System.Globalization;
using KumesScada.Application.Interfaces;
using KumesScada.Domain.Models.Excel;
using NPOI.SS.UserModel;

namespace KumesScada.Infrastructure.Excel
{
    /// <summary>
    /// Kümesin ana excel'indeki ("Yumurtlama Kartı") "VERİLER" sayfasından
    /// "HEDEF %" / "HEDEF GR" haftalık hedef satırlarını okur.
    ///
    /// NPOI kullanılır (ClosedXML DEĞİL) — bu ana excel'ler genelde eski
    /// .xls (binary/OLE2) formatında arşivlenmiş oluyor; ClosedXML sadece
    /// .xlsx (OOXML) açabiliyor, NPOI'nin WorkbookFactory'si ise hem
    /// .xls hem .xlsx'i aynı IWorkbook arayüzüyle (uzantıya değil dosya
    /// imzasına bakarak) açabiliyor.
    ///
    /// Bkz. örnek "T1 127.PARTİ SÜPER NİCK#M2.xls" / "R5 LOHMANN CL. -
    /// V126. PARTİ.xls" dosyalarının "VERİLER" sayfası:
    ///
    ///   B sütunu "%"  -> D sütunundan itibaren hafta no'ları (başlık satırı)
    ///   C sütunu "HEDEF  %" (hemen altındaki birkaç satırdan biri) ->
    ///   o haftaya karşılık gelen hedef randıman değeri.
    ///
    /// Aynı desen "GR" bölümü için "HEDEF  GR" satırında geçerlidir.
    ///
    /// Sayfada ayrıca "HEDEF" satırı bulunan 4 bölüm DAHA var — ADET
    /// (toplam yumurta hedefi), YAŞAM (yaşama oranı % hedefi), HH (HH
    /// randıman hedefi), EGG MASS (günlük yumurta kütlesi hedefi) — bkz.
    /// proje notları (T1/T14 karşılaştırması). Bunlar da okunuyor ve
    /// RaporVeriAppService ile kaydediliyor, ama şu an Grafik sekmesinde
    /// hiçbir yerde GÖSTERİLMİYOR (sadece %/GR grafik hedef çizgilerini
    /// besliyor) — bulunamazlarsa (çoğu kartta dolu değil) sessizce boş
    /// liste döner, uyarı eklenmez ("YEM" bölümünde hiç "HEDEF" satırı
    /// yok, o yüzden hiç okunmuyor).
    /// </summary>
    public class RaporVeriExcelReader : IRaporVeriExcelReader
    {
        private const string SheetName = "VERİLER";

        public RaporVeriExcelOkumaSonucu Read(Stream stream, string fileName)
        {
            var result = new RaporVeriExcelOkumaSonucu();

            IWorkbook workbook;

            try
            {
                workbook = WorkbookFactory.Create(stream);
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Excel dosyası açılamadı: {ex.Message}");
                return result;
            }

            ISheet? worksheet = null;

            for (var i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);

                if (string.Equals(sheet.SheetName.Trim(), SheetName, StringComparison.OrdinalIgnoreCase))
                {
                    worksheet = sheet;
                    break;
                }
            }

            if (worksheet == null)
            {
                result.Warnings.Add($"\"{SheetName}\" sayfası bulunamadı.");
                return result;
            }

            result.HedefYuzde = ReadHedefSatirlari(worksheet, "%");
            result.HedefGram = ReadHedefSatirlari(worksheet, "GR");
            result.HedefAdet = ReadHedefSatirlari(worksheet, "ADET");
            result.HedefYasam = ReadHedefSatirlari(worksheet, "YAŞAM");
            result.HedefHh = ReadHedefSatirlari(worksheet, "HH");
            result.HedefEggMass = ReadHedefSatirlari(
                worksheet, "EGG MASS (HAFTALIK YUM. AĞIRLIK * RANDIMAN%)");

            if (result.HedefYuzde.Count == 0)
                result.Warnings.Add("\"HEDEF %\" satırı bulunamadı/boş.");

            if (result.HedefGram.Count == 0)
                result.Warnings.Add("\"HEDEF GR\" satırı bulunamadı/boş.");

            return result;
        }

        private static List<RaporVeriHaftalikDeger> ReadHedefSatirlari(
            ISheet worksheet, string bolumAdi)
        {
            var sonuc = new List<RaporVeriHaftalikDeger>();

            var lastRowIndex = worksheet.LastRowNum; // NPOI 0-indeksli

            for (var rowIndex = 0; rowIndex <= lastRowIndex; rowIndex++)
            {
                var row = worksheet.GetRow(rowIndex);
                if (row == null) continue;

                // B sütunu (0-indeksli: 1)
                var bolumHucresi = GetCellText(row.GetCell(1));

                if (!string.Equals(bolumHucresi, bolumAdi, StringComparison.OrdinalIgnoreCase))
                    continue;

                var headerRowIndex = rowIndex;
                var hedefRowIndex = FindHedefRow(worksheet, headerRowIndex, lastRowIndex);

                if (hedefRowIndex == -1)
                    continue;

                var headerRow = worksheet.GetRow(headerRowIndex);
                var hedefRow = worksheet.GetRow(hedefRowIndex);

                var lastColumn = Math.Max(
                    headerRow?.LastCellNum ?? 0,
                    hedefRow?.LastCellNum ?? 0);

                // D sütunundan itibaren (0-indeksli: 3)
                for (var col = 3; col < lastColumn; col++)
                {
                    if (!TryGetNumeric(headerRow?.GetCell(col), out var haftaRaw))
                        continue;

                    var hafta = (int)Math.Round(haftaRaw);

                    if (hafta <= 0)
                        continue;

                    if (!TryGetDecimal(hedefRow?.GetCell(col), out var deger))
                        continue;

                    sonuc.Add(new RaporVeriHaftalikDeger
                    {
                        HaftaSayisi = hafta,
                        RaporHedefDeger = deger
                    });
                }

                // Aynı bölüm adı ("%" / "GR") sayfada normalde tekrar
                // etmez; ilk eşleşme yeterli.
                break;
            }

            return sonuc;
        }

        /// <summary>
        /// Başlık satırının hemen altındaki birkaç satır arasında, C
        /// sütunu (iç boşluklar sadeleştirilince) "HEDEF" ile başlayan
        /// satırı bulur — "HEDEF  %" / "HEDEF  GR" gibi çift boşluklu
        /// etiketleri de yakalar.
        /// </summary>
        private static int FindHedefRow(ISheet worksheet, int headerRowIndex, int lastRowIndex)
        {
            var sinir = Math.Min(headerRowIndex + 5, lastRowIndex);

            for (var candidate = headerRowIndex + 1; candidate <= sinir; candidate++)
            {
                var row = worksheet.GetRow(candidate);
                if (row == null) continue;

                // C sütunu (0-indeksli: 2)
                var label = NormalizeLabel(GetCellText(row.GetCell(2)));

                if (label.StartsWith("HEDEF", StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }

            return -1;
        }

        private static string GetCellText(ICell? cell)
        {
            if (cell == null) return string.Empty;

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,
                CellType.Numeric => cell.NumericCellValue.ToString(CultureInfo.InvariantCulture),
                CellType.Formula => GetFormulaText(cell),
                _ => string.Empty,
            };
        }

        private static string GetFormulaText(ICell cell)
        {
            try
            {
                return cell.CachedFormulaResultType switch
                {
                    CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,
                    CellType.Numeric => cell.NumericCellValue.ToString(CultureInfo.InvariantCulture),
                    _ => string.Empty,
                };
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool TryGetNumeric(ICell? cell, out double value)
        {
            value = 0;

            if (cell == null) return false;

            if (cell.CellType == CellType.Numeric)
            {
                value = cell.NumericCellValue;
                return true;
            }

            if (cell.CellType == CellType.Formula &&
                cell.CachedFormulaResultType == CellType.Numeric)
            {
                value = cell.NumericCellValue;
                return true;
            }

            var text = GetCellText(cell);
            return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryGetDecimal(ICell? cell, out decimal value)
        {
            if (TryGetNumeric(cell, out var numeric))
            {
                value = (decimal)numeric;
                return true;
            }

            value = 0;
            return false;
        }

        private static string NormalizeLabel(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return string.Join(
                ' ',
                value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
