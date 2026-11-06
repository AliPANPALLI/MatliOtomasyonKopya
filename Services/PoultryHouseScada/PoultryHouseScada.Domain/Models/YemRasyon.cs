namespace PoultryHouseScada.Domain.Models
{
    public class YemRasyon
    {
        public int Id { get; set; }
        public int LokasyonId { get; set; }
        public string? YemAdi { get; set; }
        public string? Rasyon { get; set; }
        public decimal BirimFiyat { get; set; }
        public DateTime? GecerlilikBaslangicZamani { get; set; }
        public DateTime? GecerlilikBitisZamani { get; set; }
        public bool Silindi { get; set; } = false;
    }
}
