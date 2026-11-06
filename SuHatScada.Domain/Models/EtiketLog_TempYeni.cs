namespace SuHatScada.Domain.Models
{
    public class EtiketLog_TempYeni
    {
        public long Id { get; set; }
        public int? EtiketId { get; set; }
        public int? SuKaynakId { get; set; }
        public int? SayacId { get; set; }
        public int? LokasyonId { get; set; }
        public DateTime? KayitZamani { get; set; }
        public decimal? Deger { get; set; }
    }
}
