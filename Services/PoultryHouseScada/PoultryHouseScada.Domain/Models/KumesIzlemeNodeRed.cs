namespace PoultryHouseScada.Domain.Models
{
    public class KumesIzlemeNodeRed
    {
        public long Id { get; set; }
        public DateTime? KayitZamaniNodeRed { get; set; }
        public DateTime? KayitZamani { get; set; } = new DateTime();
        public string Veriler { get; set; }
        public int KumesId { get; set; }
        public int Durum { get; set; }
    }
}
