namespace Authentication.Application.ViewModels
{
    public class ClientTokenViewModel
    {
        public string AccessToken { get; set; }
        public DateTime AccessTokenExpiration { get; set; }
    }
}
