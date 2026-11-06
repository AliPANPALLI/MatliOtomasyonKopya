namespace SharedApp.Application.ViewModels
{
    public class UserLinkYetkiViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int FormId { get; set; }
        public string Link { get; set; }
        public bool? Read { get; set; }
        public bool? Write { get; set; }
        public bool? SorumluMu { get; set; }
        public bool? Silindi { get; set; }
        public DateTime? SilinmeZamani { get; set; }
        public bool? Aktif { get; set; }
    }
}
