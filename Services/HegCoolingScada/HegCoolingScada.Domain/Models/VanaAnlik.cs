namespace HegCoolingScada.Domain.Models
{
    public class VanaAnlik
    {
        public short VanaId { get; set; }
        public bool Durum { get; set; }
        public DateTime DurumBaslangici { get; set; }
        public DateTime GuncellemeZamani { get; set; }
    }
}
