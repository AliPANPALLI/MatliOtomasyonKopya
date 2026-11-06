using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace MatliOtomasyon.Shared.Web.Security
{
    public class UserExtension : IUserExtension
    {
        private readonly IHttpContextAccessor _httpContext;

        public UserExtension(IHttpContextAccessor httpContext)
        {
            _httpContext = httpContext;
        }

        public string Name => _httpContext.HttpContext?.User?.Identity?.Name ?? "";

        public IEnumerable<Claim> GetClaimsIdentity()
        {
            return _httpContext.HttpContext.User.Claims;
        }

        public bool IsAuthenticated()
        {
            try
            {
                return _httpContext.HttpContext.User.Identity.IsAuthenticated;
            }
            catch
            {
                return false;
            }
        }
    }
}
