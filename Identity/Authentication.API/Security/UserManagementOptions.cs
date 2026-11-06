namespace Authentication.API.Security
{
    /// <summary>
    /// Kullanıcı yönetimi yetkisinin hangi SharedApp formuna bağlı olduğunu tutan ayarlar.
    /// Yetki için yeni bir tablo yok: "Kullanıcı Yönetimi" sayfası SharedApp'te bir form olarak tanımlı,
    /// kullanıcılara UserFormYetki ile Okuma/Yazma veriliyor.
    /// </summary>
    public sealed class UserManagementOptions
    {
        /// <summary>Ayarların okunduğu konfigürasyon bölümünün adı.</summary>
        public const string SectionName = "UserManagement";

        /// <summary>
        /// Kullanıcı yönetimi sayfasının Angular route'u (SharedApp'teki ModulLink + FormLink).
        /// Boşsa kullanıcı yönetimi uçları herkese kapalıdır.
        /// </summary>
        public string FormLink { get; set; } = string.Empty;

        /// <summary>Kullanıcının form yetkisinin bellekte tutulacağı süre (saniye).</summary>
        public int PermissionCacheSeconds { get; set; } = 60;
    }
}
