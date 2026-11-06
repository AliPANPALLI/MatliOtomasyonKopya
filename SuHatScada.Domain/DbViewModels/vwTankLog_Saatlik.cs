namespace SuHatScada.Domain.DbViewModels
{
    public class vwTankLog_Saatlik
    {
        public int? SuKaynakId { get; set; }
        public DateTime? Tarih { get; set; }
        public int? Saat{ get; set; }
        public DateTime? IlkZaman{ get; set; }
        public DateTime? SonZaman{ get; set; }
        public decimal? MinHacim{ get; set; }
        public decimal? OrtalamaHacim{ get; set; }
        public decimal? MaxHacim{ get; set; }
    }
}
