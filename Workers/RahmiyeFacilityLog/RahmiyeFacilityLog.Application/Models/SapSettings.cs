namespace RahmiyeFacilityLog.Application.Models
{
    /// <summary>
    /// appsettings.json içindeki "Sap" bölümüne karşılık gelir.
    /// </summary>
    public class SapSettings
    {
        /// <summary>
        /// ZWS_F0017 kantar servisinin çağrı adresi (WSDL adresi değil, servisin bindg_url'siz endpoint'i).
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        /// <summary>
        /// Servis SOAPAction bekliyorsa buraya girilebilir. Boş bırakılırsa "" (boş SOAPAction) gönderilir.
        /// </summary>
        public string? SoapAction { get; set; }
    }
}
