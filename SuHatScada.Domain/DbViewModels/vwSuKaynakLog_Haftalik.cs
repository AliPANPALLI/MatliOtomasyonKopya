namespace SuHatScada.Domain.DbViewModels
{
    public class vwSuKaynakLog_Haftalik
    {
        public int? SayacId { get; set; }
        public int? Hafta { get; set; }
        public int? Yil { get; set; }
        public decimal? IlkDeger { get; set; }
        public decimal? SonDeger { get; set; }
        public decimal? DebiFark { get; set; }
        public DateTime? IlkZaman { get; set; }
        public DateTime? SonZaman { get; set; }
    }
}
