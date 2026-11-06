using Authentication.Domain.Models;

namespace Authentication.Domain.Interfaces
{
    public interface ITwoFactorRepository
    {
        Task<string> GetCodeVerification(string userId, string ipAdres);
        Task<bool> VerifyOtpCode(string userId, string userOtpCode);
    }
}
