namespace KumesScada.Domain.Models
{
    public class KumesYumurtaDurum
    {
        public int KumesYumurtaDurumId { get; set; }

        public string KumesYumurtaDurumAd { get; set; } = null!;

        public ICollection<KumesVeri> KumesVerileri { get; set; } = new List<KumesVeri>();
    }
}
