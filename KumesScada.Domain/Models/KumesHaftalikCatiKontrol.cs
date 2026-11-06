namespace KumesScada.Domain.Models
{
    public class KumesHaftalikCatiKontrol
    {
        public int Id { get; set; }

        public int KumesId { get; set; }
        public int? DonemId { get; set; }

        public int? HaftaSayisi { get; set; }

        public decimal? Deger { get; set; }

        public DateTime? KayitZamani { get; set; }
        public string? KayitYapanUserId { get; set; }

        public DateTime? GuncellemeZamani { get; set; }
        public string? GuncelleyenUserId { get; set; }

        public bool? Silindi { get; set; }
    }
}