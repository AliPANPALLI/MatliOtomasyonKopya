namespace MobileUpdate.Domain.Entities
{
    /// <summary>
    /// Uygulamanın kurulu olduğu telefon. Her sürüm kontrolünde güncellenir; admin panelde
    /// hangi cihazın eski sürümde kaldığını izlemek için tutulur.
    /// </summary>
    public class Cihaz
    {
        /// <summary>Uygulamanın ilk açılışta üretip sakladığı kalıcı UUID.</summary>
        public string CihazId { get; set; } = null!;

        /// <summary>Bu cihazda son giriş yapan kullanıcının Id'si.</summary>
        public string? KullaniciId { get; set; }

        /// <summary>Cihazın son bildirdiği kurulu versionCode.</summary>
        public int KuruluVersionCode { get; set; }

        /// <summary>Ör. SM-A325F.</summary>
        public string? CihazModeli { get; set; }

        /// <summary>Ör. 13.</summary>
        public string? AndroidSurumu { get; set; }

        /// <summary>Son sürüm kontrolü zamanı (UTC).</summary>
        public DateTime SonGorulme { get; set; }
    }
}
