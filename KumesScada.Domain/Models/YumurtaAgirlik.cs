namespace KumesScada.Domain.Models
{
    public class YumurtaAgirlik
    {
        public int YumurtaAgirlikId { get; set; }

        public DateTime YumurtaAgirlikTarih { get; set; }

        public decimal YumurtaAgirlikMiktar { get; set; }

        public int KumeslerId { get; set; }

        public Kumesler Kumesler { get; set; } = null!;
    }
}
