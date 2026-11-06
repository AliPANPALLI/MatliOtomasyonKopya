namespace PoultryHouseScada.Domain.Models
{
    public class KumesRaporRehber
    {
        public int Id { get; set; }
        public int LokasyonId { get; set; }
        public string? RehberAdi { get; set; }
        public int HaftaSayisi { get; set; }
        public int? HayvanCinsId { get; set; }
        public decimal Deger { get; set; }
        public bool? Silindi { get; set; }
        public DateTime KayitZamani { get; set; }
        public string? KaydedenUserId { get; set; }
        public DateTime? GuncellemeZamani { get; set; }
        public string? GuncelleyenUserId { get; set; }
    }
}