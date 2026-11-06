namespace PoultryHouseScada.Domain.Models
{
    public class KumesVeri
    {
        public int KumesVeriId { get; set; }
        public DateTime Tarih { get; set; }

        public int KumeslerId { get; set; }

        public string? SiparisNo { get; set; }

        public int? KumesHayvanDetayId { get; set; }

        public decimal? KumesHayvanDetayDeger { get; set; }

        public int? KumesYumurtaDurumId { get; set; }

        public decimal? KumesYumurtaDurumDeger { get; set; }

        public int? KumesYumurtaKaliteDurumId { get; set; }

        public decimal? KumesYumurtaKaliteDurumDeger { get; set; }


        public Kumesler Kumesler { get; set; } = null!;

        public KumesHayvanDetay? KumesHayvanDetay { get; set; }

        public KumesYumurtaDurum? KumesYumurtaDurum { get; set; }

        public KumesYumurtaKaliteDurum? KumesYumurtaKaliteDurum { get; set; }
    }
}
