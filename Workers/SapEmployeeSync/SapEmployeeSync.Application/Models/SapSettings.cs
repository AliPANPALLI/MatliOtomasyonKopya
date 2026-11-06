namespace SapEmployeeSync.Application.Models
{
    /// <summary>
    /// appsettings.json içindeki "Sap" bölümüne karşılık gelir. Kullanıcı
    /// adı/şifre iki serviste de ortak (aynı SAP kullanıcısı) — her
    /// servisin kendi çağrı adresi (BaseUrl) ayrı tutuluyor.
    /// </summary>
    public class SapSettings
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public SapServiceSettings PaMatFm002 { get; set; } = new();

        public SapServiceSettings Perlist03 { get; set; } = new();

        /// <summary>
        /// Günlük senkronizasyonun çalışacağı saat (0-23, sunucu yerel
        /// saati). Varsayılan 02:00 — mesai saatleri dışında çalışsın diye.
        /// </summary>
        public int RunAtHour { get; set; } = 2;
    }

    public class SapServiceSettings
    {
        /// <summary>Servisin çağrı adresi (WSDL adresi değil, SRT binding endpoint'i).</summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>Servis SOAPAction bekliyorsa buraya girilebilir. Boş bırakılırsa "" gönderilir.</summary>
        public string? SoapAction { get; set; }
    }
}
