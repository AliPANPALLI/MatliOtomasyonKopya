using Microsoft.AspNetCore.Http;

namespace KumesScada.Application.RequestModels
{
    public sealed class VGYumurtlamaSaveRequest
    {
        /// <summary>Günlük Yem Hesapları dosyası (ölüm/yem/su/kalite)
        /// VEYA Rahmiye'nin tek dosyalı "Veri Giriş Tablosu"su.</summary>
        public IFormFile File { get; set; } = null!;

        /// <summary>
        /// Kümes Tasnif Sayımları dosyası (yumurta adedi/gramaj/kırılım).
        /// Opsiyoneldir.
        /// </summary>
        public IFormFile? TasnifFile { get; set; }

        /// <summary>
        /// Belirtilirse SADECE bu takvim yılına ait günler kaydedilir
        /// (dosya birden fazla yıla yayılmışsa filtre). Boş bırakılırsa
        /// dosyadaki TÜM yıllar kaydedilir — normal kullanımda
        /// gerekmez, tarihler zaten excel'den tam (gün/ay/yıl) okunuyor.
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// Belirtilirse yalnızca bu kümesin verileri kaydedilir.
        /// Boş bırakılırsa dosyada bulunan TÜM kümeslerin verileri
        /// kaydedilir.
        /// </summary>
        public string? KumesNo { get; set; }
    }
}
