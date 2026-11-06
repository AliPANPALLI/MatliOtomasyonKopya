using Authentication.Domain.Models;

namespace Authentication.Application.Interfaces
{
    public interface ITwoFactorService
    {
        Task<string> GetCodeVerification(string userId, string ipAdres);
        Task<bool> VerifyOtpCode(string userId, string userOtpCode);
        Task<bool> CheckOtpRequired(User user, string ipAdres);
    }
}
