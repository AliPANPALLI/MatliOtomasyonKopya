using System.Net.Http.Headers;

namespace Gateway.API
{
    public class TokenExchangeDelegateHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenExchangeDelegateHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = _httpContextAccessor.HttpContext?.Request?.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Add("Authorization", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
        //private readonly IHttpContextAccessor _httpContextAccessor;

        //public TokenExchangeDelegateHandler(IHttpContextAccessor httpContextAccessor)
        //{
        //    _httpContextAccessor = httpContextAccessor;
        //}

        //protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        //{
        //    IEnumerable<string> headerValues;
        //    if (request.Headers.TryGetValues("AccessToken", out headerValues))
        //    {
        //        string accessToken = headerValues.First();

        //        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        //        request.Headers.Remove("AccessToken");
        //    }

        //    return await base.SendAsync(request, cancellationToken);
        //}
    }

}
