using Microsoft.AspNetCore.Http;

namespace KumesScada.Application.RequestModels
{
    public sealed class VGYumurtlamaUploadRequest
    {
        /// <summary>Günlük Yem Hesapları dosyası (ölüm/yem/su/kalite).</summary>
        public IFormFile File { get; set; } = null!;

        /// <summary>
        /// Kümes Tasnif Sayımları dosyası (yumurta adedi/gramaj/kırılım).
        /// Opsiyoneldir; verilmezse rapor yalnızca ölüm/yem/su verisiyle
        /// üretilir.
        /// </summary>
        public IFormFile? TasnifFile { get; set; }
    }
}