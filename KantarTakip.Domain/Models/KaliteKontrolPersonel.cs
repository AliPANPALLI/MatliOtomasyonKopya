namespace KantarTakip.Domain.Models
{
    // Kalite kontrolü yapan personel tanımı.
    public class KaliteKontrolPersonel
    {
        public int Id { get; set; }
        public string? AdSoyad { get; set; }
        public bool? Silindi { get; set; }
        public ICollection<KantarKalite> KantarKaliteler { get; set; } = new List<KantarKalite>();
    }
}
