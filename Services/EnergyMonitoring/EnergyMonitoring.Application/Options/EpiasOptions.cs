namespace EnergyMonitoring.Application.Options
{
    /// <summary>
    /// EPİAŞ şeffaflık platformuna giriş (TGT bileti alma) için kullanılan hesap.
    /// Değerler appsettings'e yazılmaz; <c>Epias__UserName</c> ve <c>Epias__Password</c> ortam değişkenlerinden gelir.
    /// </summary>
    public sealed class EpiasOptions
    {
        /// <summary>Ayarların okunduğu konfigürasyon bölümünün adı.</summary>
        public const string SectionName = "Epias";

        /// <summary>EPİAŞ hesabının kullanıcı adı (e-posta).</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>EPİAŞ hesabının parolası.</summary>
        public string Password { get; set; } = string.Empty;
    }
}
