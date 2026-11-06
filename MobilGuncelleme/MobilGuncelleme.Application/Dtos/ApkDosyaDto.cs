namespace MobilGuncelleme.Application.Dtos
{
    /// <summary>İndirme endpoint'inin diskten stream edeceği dosya.</summary>
    public class ApkDosyaDto
    {
        /// <summary>Sunucudaki tam dosya yolu.</summary>
        public string Yol { get; set; } = null!;

        /// <summary>Telefona önerilen dosya adı, ör. matli-1.4.0.apk.</summary>
        public string IndirmeAdi { get; set; } = null!;
    }
}
