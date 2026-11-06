using Authentication.Domain.Models;

namespace Authentication.Domain.Interfaces
{
    public interface ITokenRepository
    {
        Task<UserRefreshToken> GetRefreshToken(string userId);
        Task AddUserRefreshToken(UserRefreshToken userRefreshToken);
        Task UpdateUserRefreshToken(UserRefreshToken existRefreshToken, string refreshToken, DateTime tokenExpiration);
        Task<bool> RotateRefreshToken(string userId, string expectedCode, string newCode, string expectedPasswordHash);
        Task RemoveToken(string userId);
        Task RemoveUserHeartBeat(string userId);
    }
}
