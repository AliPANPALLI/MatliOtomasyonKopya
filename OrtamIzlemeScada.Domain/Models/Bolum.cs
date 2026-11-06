namespace OrtamIzlemeScada.Domain.Models
{
    public class Bolum
    {
        public int Id { get; set; }
        public int LokasyonId { get; set; }
        public string? BolumAdi { get; set; }
        public ICollection<Oda> Odalar { get; set; } = new List<Oda>();
    }
}
