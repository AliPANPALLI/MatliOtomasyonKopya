namespace MobileUpdate.Application.Dtos
{
    /// <summary>Admin paneldeki sürüm listesinin bir satırı.</summary>
    public class SurumDto
    {
        public int Id { get; set; }
        public int VersionCode { get; set; }
        public string VersionName { get; set; } = null!;
        public string DosyaAdi { get; set; } = null!;
        public string Sha256 { get; set; } = null!;
        public long Boyut { get; set; }
        public string? SurumNotu { get; set; }
        public bool Aktif { get; set; }

        /// <summary>UTC; ekranda Türkiye saatine çevrilir.</summary>
        public DateTime YuklenmeTarihi { get; set; }
        public string? Yukleyen { get; set; }
    }
}
