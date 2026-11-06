namespace KumesScada.Domain.Models
{
    public class KaliteMukavemet
    {
        public int KaliteMukavemetId { get; set; }

        public DateTime KaliteMukavemetTarih { get; set; }

        public decimal KaliteMukavemetMiktar { get; set; }

        public int KumeslerId { get; set; }

        public Kumesler Kumesler { get; set; } = null!;
    }
}
