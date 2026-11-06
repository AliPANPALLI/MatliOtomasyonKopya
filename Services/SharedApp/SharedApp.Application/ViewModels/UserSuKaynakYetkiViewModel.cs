namespace SharedApp.Application.ViewModels
{
    public class UserSuKaynakYetkiViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public int SuKaynakId { get; set; }
        public string? SuKaynakName { get; set; }
        public int LokasyonId { get; set; }
        public string? LokasyonName { get; set; }
        public int SirketId { get; set; }
        public string? SirketName { get; set; }
        public string? CizimName { get; set; }
        public bool? Read { get; set; }
        public bool? Write { get; set; }
        public bool? Silindi { get; set; }
        public DateTime? SilinmeZamani { get; set; }
        public bool? Aktif { get; set; }
    }
}
