namespace WaterLineScada.Domain.Models
{
    public class EtiketLogYeni
    {
        public long Id { get; set; }
        public int? EtiketId { get; set; }
        public int? SuKaynakId { get; set; }
        public int? SayacId { get; set; }
        public int? LokasyonId { get; set; }
        public DateTime? KayitZamani { get; set; }
        public decimal? Deger { get; set; }

        public void Deconstruct(out DateTime? zaman, out decimal? deger)
        {
            zaman = KayitZamani;
            deger = Deger;
        }
    }
}