namespace KumesScada.Domain.Models
{
    public class Su
    {
        public int SuId { get; set; }

        public DateTime SuTarih { get; set; }

        public decimal SuMiktar { get; set; }

        public int KumeslerId { get; set; }

        public Kumesler Kumesler { get; set; } = null!;
    }
}
