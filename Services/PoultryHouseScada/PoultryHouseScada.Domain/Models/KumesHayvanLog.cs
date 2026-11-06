namespace PoultryHouseScada.Domain.Models
{
    public class KumesHayvanLog
    {
        public int Id { get; set; }
        public int? KumesHayvanId { get; set; }
        public int? KumesId { get; set; }
        public int? DonemId { get; set; }
        public DateTime? GirisZamani { get; set; }
        public DateTime? KayitZamani { get; set; }
        public string? KayitYapanUserId { get; set; }
        public short? SaglikDurumu { get; set; }
        public int? Adet { get; set; }
        public string? Aciklama { get; set; }
        public bool? Silindi { get; set; }
    }
}