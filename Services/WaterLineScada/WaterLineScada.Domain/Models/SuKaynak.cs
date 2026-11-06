namespace WaterLineScada.Domain.Models
{
    public class SuKaynak
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int LokasyonId { get; set; }
        public int? LokasyonId2 { get; set; }
        public int SuKaynakTip { get; set; }
        public int SuKaynakDetayTip { get; set; }
        public int? CalismaSureEtiketId { get; set; }
        public int? RaporSira { get; set; }
        public bool? Silindi { get; set; }
        public virtual ICollection<Sayac>? FromSayaclar { get; set; }
        public virtual ICollection<Sayac>? ToSayaclar { get; set; }
    }
}