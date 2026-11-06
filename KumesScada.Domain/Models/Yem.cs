namespace KumesScada.Domain.Models
{
    public class Yem
    {
        public int YemId { get; set; }

        public DateTime YemTarih { get; set; }

        public decimal YemMiktar { get; set; }

        public int KumeslerId { get; set; }

        public Kumesler Kumesler { get; set; } = null!;
    }
}