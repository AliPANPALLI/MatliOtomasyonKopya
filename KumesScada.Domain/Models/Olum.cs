namespace KumesScada.Domain.Models
{
    public class Olum
    {
        public int OlumId { get; set; }

        public DateTime OlumTarih { get; set; }

        public decimal OlumMiktar { get; set; }

        public int KumeslerId { get; set; }

        public Kumesler Kumesler { get; set; } = null!;
    }
}
