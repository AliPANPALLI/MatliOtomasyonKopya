namespace KumesScada.Domain.Models
{
    public class TasnifMakinesi
    {
        public int TasnifMakinesiId { get; set; }

        public string TasnifMakinesiAdi { get; set; } = null!;

        public ICollection<Kumesler> Kumesler { get; set; } = new List<Kumesler>();
    }
}
