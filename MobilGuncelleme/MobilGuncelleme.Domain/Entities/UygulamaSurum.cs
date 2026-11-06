namespace MobilGuncelleme.Domain.Entities
{
    /// <summary>
    /// Admin panelden yüklenen ve sahadaki telefonlara dağıtılan bir APK sürümü.
    /// APK dosyasının kendisi diskte durur; bu kayıt yalnızca dosya adını, hash'ini ve
    /// dağıtım durumunu tutar.
    /// </summary>
    public class UygulamaSurum
    {
        public int Id { get; set; }

        /// <summary>
        /// Android'in sürüm karşılaştırmasında kullandığı tam sayı (build.gradle versionCode).
        /// Her yayında artmak zorundadır; güncelleme kararı yalnızca bu alana bakar.
        /// </summary>
        public int VersionCode { get; set; }

        /// <summary>Kullanıcıya gösterilen sürüm adı, ör. 1.4.0.</summary>
        public string VersionName { get; set; } = null!;

        /// <summary>APK klasöründeki dosya adı, ör. matli-1.4.0-14.apk.</summary>
        public string DosyaAdi { get; set; } = null!;

        /// <summary>
        /// Yükleme sırasında sunucunun hesapladığı SHA-256 (küçük harf hex). Telefon indirdiği
        /// dosyayı bununla doğrular; bozuk veya yarım inen dosya kurulmaz.
        /// </summary>
        public string Sha256 { get; set; } = null!;

        /// <summary>Dosya boyutu (byte).</summary>
        public long Boyut { get; set; }

        /// <summary>Güncelleme ekranında kullanıcıya gösterilen yenilikler.</summary>
        public string? SurumNotu { get; set; }

        /// <summary>
        /// Pasif sürüm dağıtılmaz ve indirilemez. Hatalı bir sürüm yayınlandığında
        /// dağıtımı durdurmak için kullanılır.
        /// </summary>
        public bool Aktif { get; set; }

        /// <summary>Yüklenme zamanı (UTC). Npgsql timestamptz kolonuna yalnızca UTC kabul eder.</summary>
        public DateTime YuklenmeTarihi { get; set; }

        /// <summary>Yükleyen kullanıcının adı (token'dan).</summary>
        public string? Yukleyen { get; set; }
    }
}
