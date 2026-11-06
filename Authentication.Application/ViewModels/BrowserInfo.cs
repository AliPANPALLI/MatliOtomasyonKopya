namespace Authentication.Application.ViewModels
{
    public class BrowserInfo
    {
        public string UserAgent { get; set; }
        public string Platform { get; set; }
        public string AppName { get; set; }
        public string AppVersion { get; set; }
        public string Vendor { get; set; }
        public string Language { get; set; }
        public bool IsMobile { get; set; }
        public string BrowserName { get; set; }
        public string BrowserVersion { get; set; }
        //public string PublicIp { get; set; }
        //public string LocalIp { get; set; }
    }
}