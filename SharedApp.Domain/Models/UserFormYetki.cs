namespace SharedApp.Domain.Models
{
    public class UserFormYetki
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        //public int ModulId { get; set; }

        public int FormId { get; set; }

        public bool? Okuma { get; set; }

        public bool? Yazma { get; set; }

        public bool? Degistirme { get; set; }
        public bool? Bildirim { get; set; }

        public bool? Silme { get; set; }
        public bool? Silindi { get; set; }
        public DateTime? SilinmeZamani { get; set; }
    }
}
