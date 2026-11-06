namespace Authentication.Domain.Models
{
    public class PageVisit
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? PageName { get; set; }
        public string? ModulLink { get; set; }
        public string? FormLink { get; set; }
        public string? UrlId { get; set; }
        public DateTime? ZiyaretZamani { get; set; }
        public DateTime? CikisZamani { get; set; }
        public string? TabId { get; set; }
    }
}