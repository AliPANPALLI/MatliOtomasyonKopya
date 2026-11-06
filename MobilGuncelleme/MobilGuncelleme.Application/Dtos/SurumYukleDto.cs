using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MobilGuncelleme.Application.Dtos
{
    /// <summary>
    /// Admin panelden multipart/form-data olarak gelen yeni sürüm yüklemesi.
    /// </summary>
    public class SurumYukleDto
    {
        /// <summary>Release APK dosyası.</summary>
        [Required]
        public IFormFile Apk { get; set; } = null!;

        /// <summary>
        /// Gösterim adı, ör. 1.4.0. Dosya adına girdiği için yalnızca harf, rakam, nokta,
        /// tire ve alt çizgi kabul edilir.
        /// </summary>
        [Required, MaxLength(20), RegularExpression(@"^[0-9A-Za-z._-]+$", ErrorMessage = "VersionName yalnızca harf, rakam, nokta, tire ve alt çizgi içerebilir.")]
        public string VersionName { get; set; } = null!;

        /// <summary>build.gradle'daki versionCode. Son yüklenen sürümünkinden büyük olmalı.</summary>
        [Range(1, int.MaxValue)]
        public int VersionCode { get; set; }

        public string? SurumNotu { get; set; }

        /// <summary>
        /// Varsayılan true: yüklenen sürüm hemen dağıtıma girer ve eski sürümdeki herkes bir
        /// sonraki girişte güncellemeye zorlanır. false verilirse sürüm pasif yüklenir, önce
        /// test edilip sonra aktifleştirilebilir.
        /// </summary>
        public bool Aktif { get; set; } = true;
    }
}
