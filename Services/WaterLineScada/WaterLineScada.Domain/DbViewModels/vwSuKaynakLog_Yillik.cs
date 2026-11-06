namespace WaterLineScada.Domain.DbViewModels
{
    public class vwSuKaynakLog_Yillik
    {
        public int? SayacId { get; set; }
        public int? Yil { get; set; }
        public decimal? IlkDeger { get; set; }
        public decimal? SonDeger { get; set; }
        public decimal? DebiFark { get; set; }
        public DateTime? IlkZaman { get; set; }
        public DateTime? SonZaman { get; set; }
    }
}
