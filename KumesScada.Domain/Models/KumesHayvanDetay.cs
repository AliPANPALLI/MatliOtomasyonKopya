namespace KumesScada.Domain.Models
{
    public class KumesHayvanDetay
    {
        public int KumesHayvanDetayId { get; set; }

        public string KumesHayvanDetayAdi { get; set; } = null!;

        public ICollection<KumesVeri> KumesVerileri { get; set; } = new List<KumesVeri>();
    }
}
