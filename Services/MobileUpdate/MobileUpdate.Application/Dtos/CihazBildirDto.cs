using System.ComponentModel.DataAnnotations;

namespace MobileUpdate.Application.Dtos
{
    /// <summary>
    /// Mobil uygulamanın girişten sonra gönderdiği tek istek (POST /api/Surum/Kontrol).
    /// Hem cihazın kurulu sürümünü kaydettirir hem de güncelleme kararını ister.
    /// </summary>
    public class CihazBildirDto
    {
        /// <summary>Uygulamanın ilk açılışta ürettiği kalıcı UUID.</summary>
        [Required, MaxLength(64)]
        public string CihazId { get; set; } = null!;

        /// <summary>Telefonda kurulu sürümün versionCode'u (DeviceInfo.getBuildNumber()).</summary>
        [Range(0, int.MaxValue)]
        public int VersionCode { get; set; }

        [MaxLength(100)]
        public string? CihazModeli { get; set; }

        [MaxLength(20)]
        public string? AndroidSurumu { get; set; }
    }
}
