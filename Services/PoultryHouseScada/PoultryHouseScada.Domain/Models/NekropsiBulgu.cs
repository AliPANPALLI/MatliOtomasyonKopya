namespace PoultryHouseScada.Domain.Models
{
    public class NekropsiBulgu
    {
        public int Id { get; set; }

        public int NekropsiRaporuId { get; set; }
        public NekropsiRaporu? NekropsiRaporu { get; set; }

        public string Key { get; set; } = string.Empty;

        public int? Bulunan { get; set; }
        public int? Toplam { get; set; }
    }
}
