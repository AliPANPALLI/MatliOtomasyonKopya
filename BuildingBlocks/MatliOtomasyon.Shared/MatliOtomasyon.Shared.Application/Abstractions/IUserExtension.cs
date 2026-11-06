using System.Security.Claims;

namespace MatliOtomasyon.Shared.Application.Abstractions
{
    public interface IUserExtension
    {
        string Name { get; }
        IEnumerable<Claim> GetClaimsIdentity();
        bool IsAuthenticated();
    }
}