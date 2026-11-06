namespace KumesScada.Domain.Models
{
    public class CanliAgirlik
    {
        public int CanliAgirlikId { get; set; }

        public DateTime CanliAgirlikTarih { get; set; }

        public decimal CanliAgirlikMiktar { get; set; }

        public int KumeslerId { get; set; }

        public Kumesler Kumesler { get; set; } = null!;
    }
}
