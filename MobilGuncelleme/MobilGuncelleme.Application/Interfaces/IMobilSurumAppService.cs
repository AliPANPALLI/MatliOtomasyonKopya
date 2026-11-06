using MobilGuncelleme.Application.Dtos;

namespace MobilGuncelleme.Application.Interfaces
{
    /// <summary>Mobil sürüm dağıtımının iş kuralları.</summary>
    public interface IMobilSurumAppService
    {
        /// <summary>
        /// Cihazı kaydeder/günceller ve güncelleme gerekip gerekmediğini döner. Her güncelleme
        /// zorunludur: kurulu versionCode son aktif sürümden küçükse güncelleme vardır.
        /// </summary>
        Task<SurumKontrolSonucDto> KontrolAsync(CihazBildirDto dto, string? kullaniciId);

        /// <summary>Aktif bir sürümün indirilecek dosyası; sürüm yoksa, pasifse veya dosya diskte yoksa null.</summary>
        Task<ApkDosyaDto?> IndirmeDosyasiAsync(int surumId);

        /// <summary>APK'yı diske yazar, hash'ini hesaplar ve sürüm kaydını ekler.</summary>
        Task<SurumDto> YukleAsync(SurumYukleDto dto, string? yukleyen, CancellationToken cancellationToken = default);

        /// <summary>Tüm sürümler, en yeni versionCode önce.</summary>
        Task<List<SurumDto>> SurumleriListeleAsync();

        /// <summary>Sürümü aktif/pasif yapar. Pasif sürüm dağıtılmaz.</summary>
        Task<SurumDto> AktiflikAyarlaAsync(int surumId, bool aktif);

        /// <summary>Cihazlar ve kurulu sürümleri, en son görülen önce.</summary>
        Task<List<CihazDto>> CihazlariListeleAsync();
    }
}
