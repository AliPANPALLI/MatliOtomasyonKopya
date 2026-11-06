using PoultryHouseScada.Domain.Models.Excel;

namespace PoultryHouseScada.Application.Interfaces
{
    /// <summary>
    /// Rahmiye çiftliği "Veri Giriş Tablosu.xlsx" formatını okur.
    /// Tek dosyada 10 sayfa var (Ölü Tablosu, Günlük Yumurta Adeti,
    /// Yumurta Gramajı, Canlı Ağırlık, Yem Tüketim Tablosu (+HB GR),
    /// Su Tüketim Tablosu, Tek Sıra Randımanlar, VBAP RAND.,
    /// Uygulamalar); her sayfa satır=tarih (B sütunu), sütun=kümes
    /// (2. satır, C sütunundan itibaren) matrisidir.
    ///
    /// NOT: Bu format sağlam/kırık/kirli/küçük kırık/fire kırılımı
    /// TUTMAZ — yalnızca günlük toplam yumurta adedi vardır
    /// (VGExcelKumesGunlukData.YumurtaAdeti). Kabuk Direnci ve HU için
    /// de hiçbir sayfa yok.
    /// </summary>
    public interface IVGVeriGirisTablosuExcelReader
    {
        Task<VGExcelImportResult> ReadAsync(
            Stream stream,
            string fileName,
            int? year = null,
            CancellationToken cancellationToken = default);
    }
}
