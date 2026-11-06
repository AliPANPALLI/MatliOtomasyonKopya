namespace Authentication.Application.ViewModels
{
    public class OtpViewModel
    {
        public string ChallengeId { get; set; }
        public string OtpCode { get; set; }
        public BrowserInfo BrowserInfo { get; set; }
    }
}