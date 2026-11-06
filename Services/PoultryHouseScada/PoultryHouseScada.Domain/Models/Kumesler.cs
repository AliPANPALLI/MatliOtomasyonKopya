namespace PoultryHouseScada.Domain.Models
{
    public class Kumesler
    {
        public int Id { get; set; }
        public int? KumesId { get; set; }
        public int? LokasyonId { get; set; }
        public int? TasnifMakinesiId { get; set; }
        public string? KumesNo { get; set; }
        public string? KumesName { get; set; }
        public bool? Aktif { get; set; }
        public bool? Silindi { get; set; }

        public TasnifMakinesi? TasnifMakinesi { get; set; }

        public ICollection<Olum> Olumler { get; set; } = new List<Olum>();
        public ICollection<Yem> Yemler { get; set; } = new List<Yem>();
        public ICollection<Su> Sular { get; set; } = new List<Su>();

        public ICollection<KaliteGizliCatlak> KaliteGizliCatlaklar { get; set; } = new List<KaliteGizliCatlak>();

        public ICollection<KaliteMukavemet> KaliteMukavemetler { get; set; } = new List<KaliteMukavemet>();

        public ICollection<CanliAgirlik> CanliAgirliklar { get; set; } = new List<CanliAgirlik>();

        public ICollection<YumurtaAgirlik> YumurtaAgirliklar { get; set; } = new List<YumurtaAgirlik>();

        public ICollection<Randiman> Randimanlar { get; set; } = new List<Randiman>();

        public ICollection<VbapRandiman> VbapRandimanlar { get; set; } = new List<VbapRandiman>();

        public ICollection<Uygulama> Uygulamalar { get; set; } = new List<Uygulama>();

        public ICollection<KumesVeri> KumesVerileri { get; set; } = new List<KumesVeri>();

        public ICollection<YumurtlamaDonem> YumurtlamaDonemleri { get; set; } = new List<YumurtlamaDonem>();
    }
}