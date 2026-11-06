namespace KumesScada.Domain.Models
{
    public class NekropsiFotograf
    {
        public int Id { get; set; }

        public int NekropsiRaporuId { get; set; }
        public NekropsiRaporu? NekropsiRaporu { get; set; }

        public string DosyaAdi { get; set; } = string.Empty;
        public string GoreliDosyaYolu { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long BoyutByte { get; set; }

        public DateTime KayitZamani { get; set; }
    }
}
