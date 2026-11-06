namespace KumesScada.Domain.Models
{
    public class KumesYemRasyonEsleme
    {
        public int Id { get; set; }
        public int KumesId { get; set; }
        public int LokasyonId { get; set; }
        public int YemRasyonId { get; set; }
        public DateTime? KayitZamani { get; set; }
        public DateTime? GecerlilikBaslangicZamani { get; set; }
        public DateTime? GecerlilikBitisZamani { get; set; }
        public bool Silindi { get; set; } = false;
    }
}
