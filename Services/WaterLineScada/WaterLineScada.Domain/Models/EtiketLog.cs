namespace WaterLineScada.Domain.Models
{
    public class EtiketLog
    {
        public int Id { get; set; }
        public int? EtiketId { get; set; }
        public int? SuKaynakId { get; set; }
        public int? SayacId { get; set; }
        public int? LokasyonId { get; set; }
        public DateTime? KayitZamani { get; set; }
        public decimal? Deger { get; set; }

        // Deconstruct metodunu ekleyelim
        public void Deconstruct(out DateTime? zaman, out decimal? deger)
        {
            zaman = KayitZamani;
            deger = Deger;
        }
    }
}