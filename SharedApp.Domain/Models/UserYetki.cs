namespace SharedApp.Domain.Models
{
    public class UserYetki
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int FormId { get; set; }
        public string Link { get; set; }
        public bool? Okuma { get; set; }
        public bool? Yazma { get; set; }
        public bool? Degistirme { get; set; }
        public bool? Silme { get; set; }
        public bool? SorumluMu { get; set; }
        public bool? Onaylayabilir { get; set; }
        public bool? Silindi { get; set; }
        public DateTime? SilinmeZamani { get; set; }
        public bool? Aktif { get; set; }
    }
}
