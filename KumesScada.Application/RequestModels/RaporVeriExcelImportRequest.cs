using Microsoft.AspNetCore.Http;

namespace KumesScada.Application.RequestModels
{
    public sealed class RaporVeriExcelImportRequest
    {
        /// <summary>
        /// Kümesin "Yumurtlama Kartı" ana excel'i (örn. "T1 127.PARTİ
        /// SÜPER NİCK#M2.xlsx" / "R5 LOHMANN CL. - V126. PARTİ.xlsx") —
        /// "VERİLER" sayfasındaki "HEDEF %" / "HEDEF GR" satırları okunur.
        /// </summary>
        public IFormFile File { get; set; } = null!;

        /// <summary>
        /// Hedef eğrisinin hangi kümese ait olduğu — kategori adına
        /// eklenir (bkz. RaporVeriAppService.ImportFromExcelAsync),
        /// çünkü hedef eğrisi kümesten kümese (ırka göre) farklılık
        /// gösterebilir; tek bir ortak eğri yeterli değildir.
        /// </summary>
        public string KumesNo { get; set; } = string.Empty;
    }
}
