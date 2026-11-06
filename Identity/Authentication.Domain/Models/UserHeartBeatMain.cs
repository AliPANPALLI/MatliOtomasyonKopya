namespace Authentication.Domain.Models
{
    public class UserHeartBeatMain
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? PageName { get; set; }
        public string? ModulLink { get; set; }
        public string? FormLink { get; set; }
        public string? UrlId { get; set; }
        public DateTime? LastSeen { get; set; }
        public string? TabId { get; set; }
        //Otomatik Temizlik
        //DELETE FROM dbo.UserHeartbeat WHERE LastSeen < DATEADD(HOUR, -24, GETUTCDATE())
    }
}
