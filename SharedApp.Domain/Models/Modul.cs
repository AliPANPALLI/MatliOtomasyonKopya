namespace SharedApp.Domain.Models
{
    public class Modul
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
        public int? Sira { get; set; }
        public string? YetkiTablosu { get; set; }
        public bool? Silindi { get; set; }
    }
}
