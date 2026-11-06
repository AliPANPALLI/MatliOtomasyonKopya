namespace MobileUpdate.Application.Dtos
{
    /// <summary>Admin paneldeki cihaz listesinin bir satırı.</summary>
    public class CihazDto
    {
        public string CihazId { get; set; } = null!;
        public string? KullaniciId { get; set; }
        public int KuruluVersionCode { get; set; }
        public string? CihazModeli { get; set; }
        public string? AndroidSurumu { get; set; }

        /// <summary>UTC; ekranda Türkiye saatine çevrilir.</summary>
        public DateTime SonGorulme { get; set; }

        /// <summary>
        /// Kurulu sürüm son aktif sürümden küçükse false. Panel eski sürümde kalan
        /// cihazları bu alana göre renkle işaretler.
        /// </summary>
        public bool GuncelMi { get; set; }
    }
}
