namespace KumesScada.Domain.Models
{
    public class EtiketLogKonveyor
    {
        public int Id { get; set; }
        public int? EtiketId { get; set; }
        public int? LokasyonId { get; set; }
        public DateTime? KayitZamani { get; set; }
        public decimal? Deger { get; set; }
    }
}
