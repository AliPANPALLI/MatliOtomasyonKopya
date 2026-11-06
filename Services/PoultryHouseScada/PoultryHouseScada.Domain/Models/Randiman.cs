namespace PoultryHouseScada.Domain.Models
{
    public class Randiman
    {
        public int RandimanId { get; set; }

        public DateTime RandimanTarih { get; set; }

        public decimal RandimanMiktar { get; set; }

        public int KumeslerId { get; set; }

        public Kumesler Kumesler { get; set; } = null!;
    }
}
