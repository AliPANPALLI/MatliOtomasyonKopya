namespace KumesScada.Domain.Models
{
    public class EtiketLog_TempYeni
    {
        public long Id { get; set; }
        public int? EtiketId { get; set; }
        public int? KumesId { get; set; }
        public int? KonveyorId { get; set; }
        public DateTime? KayitZamani { get; set; }
        public decimal? Deger { get; set; }
    }
}
