namespace Authentication.Domain.Models.ReportModels
{
    public class TabCloseDto
    {
        public string TabId { get; set; }
        //public DateTime LastActivity { get; set; }
        public string CurrentPage { get; set; }
        public string Token { get; set; }
    }
}