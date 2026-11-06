namespace SuHatScada.Domain.Models
{
    public class CizimLokasyonEsleme
    {
        public int Id { get; set; }
        public int CizimId { get; set; }
        public int LokasyonId { get; set; }
        public bool? Silindi { get; set; }
    }
}