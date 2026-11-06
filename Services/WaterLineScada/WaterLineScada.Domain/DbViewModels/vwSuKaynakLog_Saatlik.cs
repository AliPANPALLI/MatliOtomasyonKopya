namespace WaterLineScada.Domain.DbViewModels
{
    public class vwSuKaynakLog_Saatlik
    {
        public int? SayacId { get; set; }
        public DateTime? Tarih { get; set; }
        public int? Saat { get; set; }
        public DateTime? IlkZaman { get; set; }
        public DateTime? SonZaman { get; set; }
        public decimal? DebiFark { get; set; }
        public decimal? IlkDeger { get; set; }
        public decimal? SonDeger { get; set; }
    }
}
