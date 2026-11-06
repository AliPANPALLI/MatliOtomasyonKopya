namespace SharedApp.Domain.Models
{
    public class SonGelistirmeler
    {
        public int Id { get; set; }
        public string? VersiyonNo { get; set; }
        public string? Aciklama { get; set; }
        public DateTime? Tarih { get; set; }
    }
}