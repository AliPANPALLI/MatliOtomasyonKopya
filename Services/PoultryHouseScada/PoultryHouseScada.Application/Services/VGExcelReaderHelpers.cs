using ClosedXML.Excel;
using PoultryHouseScada.Domain.Models.Excel;

namespace PoultryHouseScada.Infrastructure.Excel
{
    /// <summary>
    /// VG excel okuyucuları arasında paylaşılan yardımcı metodlar
    /// (<see cref="VGExcelReader"/> ve <see cref="VGKumesTasnifExcelReader"/>).
    /// </summary>
    internal static class VGExcelReaderHelpers
    {
        public static VGExcelKumesGunlukData GetOrCreate(
            Dictionary<(DateTime Tarih, string KumesNo),
                VGExcelKumesGunlukData> dictionary,
            DateTime tarih,
            string kumesNo)
        {
            var key = (tarih.Date, kumesNo);

            if (dictionary.TryGetValue(key, out var existing))
                return existing;

            var data = new VGExcelKumesGunlukData
            {
                Tarih = tarih.Date,
                KumesNo = kumesNo
            };

            dictionary.Add(key, data);

            return data;
        }

        public static bool TryGetDecimal(
            IXLCell cell,
            out decimal value)
        {
            value = 0;

            if (cell.IsEmpty())
                return false;

            if (cell.TryGetValue<decimal>(out var decimalValue))
            {
                value = decimalValue;
                return true;
            }

            if (decimal.TryParse(
                    cell.GetFormattedString(),
                    out decimalValue))
            {
                value = decimalValue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Türkçe karakterleri sadeleştirip boşluk/noktalama
        /// işaretlerini atarak karşılaştırmayı kolaylaştırır.
        /// Örn: "Yum.Ort.Ağ.(gr)" -> "YUMORTAGGR".
        /// </summary>
        public static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var upper = value
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
                .Replace("Ç", "C");

            var chars = upper
                .Where(char.IsLetterOrDigit)
                .ToArray();

            return new string(chars);
        }
    }
}
