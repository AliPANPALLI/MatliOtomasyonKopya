namespace PoultryHouseScada.Domain.Models
{
    public class RaporKategori
    {
        public int RaporKategoriId { get; set; }

        public string RaporKategoriAd { get; set; } = null!;

        public ICollection<RaporVeri> RaporVerileri { get; set; } = new List<RaporVeri>();
    }
}
