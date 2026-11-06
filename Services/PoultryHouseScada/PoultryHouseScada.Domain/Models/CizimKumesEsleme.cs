namespace PoultryHouseScada.Domain.Models
{
    public class CizimKumesEsleme
    {
        public int Id { get; set; }
        public int? CizimId { get; set; }
        public int? KumesId { get; set; }
        public int? SekilId { get; set; }
        public bool? Silindi { get; set; }
    }
}