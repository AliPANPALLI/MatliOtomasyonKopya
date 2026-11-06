namespace KumesScada.Domain.Models
{
    public class KumesYumurtaKaliteDurum
    {
        public int KumesYumurtaKaliteDurumId { get; set; }

        public string KumesYumurtaKaliteDurumAd { get; set; } = null!;

        public ICollection<KumesVeri> KumesVerileri { get; set; } = new List<KumesVeri>();
    }
}
