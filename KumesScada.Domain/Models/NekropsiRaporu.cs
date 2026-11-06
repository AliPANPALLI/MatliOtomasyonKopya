namespace KumesScada.Domain.Models
{
    public class NekropsiRaporu
    {
        public int Id { get; set; }

        public int KumesId { get; set; }

        public DateTime Tarih { get; set; }
        public int? Hafta { get; set; }

        public string NekropsiSebebi { get; set; } = string.Empty;
        public string Pcr { get; set; } = "BELIRTILMEDI";

        public DateTime KayitZamani { get; set; }
        public string? KayitYapanUserId { get; set; }

        public bool? Silindi { get; set; }

        public ICollection<NekropsiBulgu> Bulgular { get; set; } = new List<NekropsiBulgu>();
        public ICollection<NekropsiFotograf> Fotograflar { get; set; } = new List<NekropsiFotograf>();
    }
}
