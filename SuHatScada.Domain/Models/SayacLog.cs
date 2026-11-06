namespace SuHatScada.Domain.Models
{
    public class SayacLog
    {
        public int Id { get; set; }
        public int SayacId { get; set; }
        public DateTime KayitZamani { get; set; }
        public decimal? TotalDebi { get; set; }
        public decimal? Debi { get; set; }
        /// <summary>
        /// (metre)(-20:+80)(birim)(sınır aralığı)
        /// </summary>
        public decimal? Hiz { get; set; }
    }
}
