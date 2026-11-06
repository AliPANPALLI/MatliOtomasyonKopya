namespace SuHatScada.Domain.Models
{
    public class SuKaynakLog_TempYeni
    {
        public long Id { get; set; }
        public int? SuKaynakId { get; set; }
        public DateTime KayitZamani { get; set; }
        public decimal? Basinc { get; set; }
        public decimal? SeviyeMetre { get; set; }
        public decimal? SeviyeYuzde { get; set; }
        public decimal? SuHacim { get; set; }
        public decimal? SuHacimYuzde { get; set; }
        public decimal? TankHacim { get; set; }
        public int? SayacId { get; set; }
        public decimal? Debi { get; set; }
        public decimal? DebiSayac { get; set; }
    }
}