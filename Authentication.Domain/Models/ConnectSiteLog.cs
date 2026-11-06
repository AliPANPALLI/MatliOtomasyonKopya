namespace Authentication.Domain.Models
{
    public class ConnectSiteLog
    {
        public int Id { get; set; }
        public string? LocalName { get; set; }
        public string? HostName { get; set; }
        public string? Browser { get; set; }
        public string? BrowserVersion { get; set; }
        public bool? IsMobile { get; set; }
        public string? NetIp { get; set; }
        public string? LocalIp { get; set; }
        public string? Page { get; set; }
        public DateTime? Tarih { get; set; }
        public string? UserId { get; set; }
        public string? Aciklama { get; set; }
    }
}