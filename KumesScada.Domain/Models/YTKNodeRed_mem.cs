namespace KumesScada.Domain.Models
{
    public class YTKNodeRed_mem
    {
        public long Id { get; set; }
        public DateTime? KayitZamaniNodeRed { get; set; }
        public DateTime? KayitZamani { get; set; } = new DateTime();
        public string Veriler { get; set; }
        public int LokasyonId { get; set; }
        public int Durum { get; set; }
        public string? PlcAdi { get; set; }
    }
}
