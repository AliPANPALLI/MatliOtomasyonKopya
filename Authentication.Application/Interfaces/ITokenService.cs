using Authentication.Application.ViewModels;
using Authentication.Domain.Models;

namespace Authentication.Application.Interfaces
{
    public interface ITokenService
    {
        Task<TokenViewModel> CreateToken(User user, bool isRefreshing);
        Task<TokenViewModel> CreateNewRefreshToken(User user, string refreshToken);
        Task RemoveToken(string userId);
        Task RemoveUserHeartBeat(string userId);
    }
}
