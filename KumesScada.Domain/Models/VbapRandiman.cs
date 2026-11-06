namespace KumesScada.Domain.Models
{
 public class VbapRandiman 
    {
        public int VbapRandimanId { get; set; }

        public DateTime VbapRandimanTarih { get; set; }

        public decimal VbapRandimanMiktar { get; set; }
        public int KumeslerId { get; set; }

        public Kumesler Kumesler { get; set; } = null!;
    }
}
