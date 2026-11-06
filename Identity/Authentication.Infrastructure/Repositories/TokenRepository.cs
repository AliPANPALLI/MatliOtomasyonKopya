using Authentication.Domain.Interfaces;
using Authentication.Domain.Models;
using Microsoft.EntityFrameworkCore;


namespace Authentication.Infrastructure.Repositories
{
    public class TokenRepository : ITokenRepository
    {
        private AuthDbContext _context;
        public TokenRepository(AuthDbContext context)
        {
            _context = context;
        }
        public async Task<bool> RotateRefreshToken(string userId, string expectedCode, string newCode, string expectedPasswordHash)
        {
            var now = DateTime.Now;
            var updated = await _context.UserRefreshTokens
                .Where(x => x.UserId == userId && x.Code == expectedCode && x.Expiration > now &&
                    _context.User.Any(u => u.Id == userId && u.PasswordHash == expectedPasswordHash && u.Silindi != true && u.IsLocked != true && (u.IncorrectLoginNumber ?? 0) < 5))
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Code, newCode));
            return updated == 1;
        }
        public async Task RemoveToken(string userId)
        {
            var existRefreshToken = await _context.UserRefreshTokens.Where(x => x.UserId == userId).FirstOrDefaultAsync();
            if (existRefreshToken != null)
            {
                _context.UserRefreshTokens.Remove(existRefreshToken);
                await _context.SaveChangesAsync();
            }
        }
        public async Task RemoveUserHeartBeat(string userId)
        {
            var existHeartBeatList = await _context.UserHeartBeatMain.Where(x => x.UserId == userId).ToListAsync();
            foreach (var userHeartBeat in existHeartBeatList)
            {
                _context.UserHeartBeatMain.Remove(userHeartBeat);
            }

            var existPageVisitMainList = await _context.PageVisitMain.Where(x => x.UserId == userId && x.CikisZamani == null).ToListAsync();
            foreach (var pageVisitMain in existPageVisitMainList)
            {
                pageVisitMain.CikisZamani = DateTime.Now;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<UserRefreshToken> GetRefreshToken(string userId)
        {
            return await _context.UserRefreshTokens.Where(x => x.UserId == userId).FirstOrDefaultAsync();
        }
        public async Task AddUserRefreshToken(UserRefreshToken userRefreshToken)
        {
            await _context.UserRefreshTokens.AddAsync(userRefreshToken);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateUserRefreshToken(UserRefreshToken existRefreshToken, string refreshToken, DateTime tokenExpiration)
        {
            existRefreshToken.Code = refreshToken;
            existRefreshToken.Expiration = tokenExpiration;
            await _context.SaveChangesAsync();
        }
    }
}
