namespace MatliOtomasyon.Shared.Infrastructure.Configuration
{
    /// <summary>
    /// Servislerin birbirini kullanıcı token'ı olmadan çağırdığı durumlar için geçici servis anahtarı ayarları.
    /// Şu an sadece SharedApp'in Authentication API'deki kullanıcı listesini çekmesi için kullanılıyor;
    /// OpenIddict client_credentials akışına geçildiğinde (Faz 7) kaldırılacak.
    /// Anahtar appsettings'e yazılmaz, ortam değişkeninden okunur (InternalServices__AuthenticationApiKey).
    /// </summary>
    public sealed class InternalServiceOptions
    {
        /// <summary>Ayarların okunduğu konfigürasyon bölümünün adı.</summary>
        public const string SectionName = "InternalServices";

        /// <summary>Servis anahtarının taşındığı HTTP header'ının adı.</summary>
        public const string ApiKeyHeaderName = "X-Internal-Api-Key";

        /// <summary>
        /// Authentication API'nin iç servis çağrılarında beklediği anahtar.
        /// Boşsa iç servis erişimi tamamen kapalıdır.
        /// </summary>
        public string AuthenticationApiKey { get; set; } = string.Empty;
    }
}
