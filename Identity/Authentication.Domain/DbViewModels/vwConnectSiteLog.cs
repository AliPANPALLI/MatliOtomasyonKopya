namespace Authentication.Domain.DbViewModels
{
    public class vwConnectSiteLog
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? LocalName { get; set; }
        public string? HostName { get; set; }
        public string? Browser { get; set; }
        public string? BrowserVersion { get; set; }
        public bool? IsMobile { get; set; }
        public string? NetIp { get; set; }
        public string? LocalIp { get; set; }
        public DateTime? Tarih { get; set; }
        public string? Aciklama { get; set; }

        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? UserFullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? UserName { get; set; }
        public byte[]? ProfilePhoto { get; set; }
        public string? Position { get; set; }
        public bool? Silindi { get; set; }
        public string? LokasyonName { get; set; }
        public string? SirketName { get; set; }
    }
}