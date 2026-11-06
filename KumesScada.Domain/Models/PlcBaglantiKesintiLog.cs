namespace KumesScada.Domain.Models
{
    public class PlcBaglantiKesintiLog
    {
        public long Id { get; set; }
        public int KumesId { get; set; }
        public DateTime? KopmaZamani { get; set; }
        public DateTime? BaglanmaZamani { get; set; }
        public long? KesintiSuresiSaniye { get; set; }
        public DateTime? KayitZamani { get; set; }
    }
}
