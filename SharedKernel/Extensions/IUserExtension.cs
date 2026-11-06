using System.Security.Claims;

namespace SharedKernel.Extensions
{
    public interface IUserExtension
    {
        string Name { get; }
        IEnumerable<Claim> GetClaimsIdentity();
        bool IsAuthenticated();
    }
}