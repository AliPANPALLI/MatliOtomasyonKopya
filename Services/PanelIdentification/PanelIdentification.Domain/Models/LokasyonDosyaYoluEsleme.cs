namespace PanelIdentification.Domain.Models
{
    public class LokasyonDosyaYoluEsleme
    {
        public int Id { get; set; }
        public string? DosyaYolu { get; set; }
        public int LokasyonId { get; set; }
        public bool? Silindi { get; set; }
    }
}