using Authentication.Domain.DbViewModels;
using Authentication.Domain.Models;
using Microsoft.AspNetCore.Http;
using SharedApp.Domain.Models;

namespace Authentication.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task CreateUser(User user, string password);
        Task<User> GetUserByName(string name);
        Task<User> GetUserBySicilNo(string sicilNo);
        Task<User> GetUserById(string id);
        Task<bool> CheckPasswordAsync(User user, string password);
        Task ChangePassword(User user, string password);
        Task<bool> TryResetPassword(string userId, string expectedPasswordHash, string newPassword);
        Task<bool> ChangeProfilePhoto(User user, IFormFile? formFile);
        Task<string> ResetPassword(User user);
        Task<bool> DeleteProfilePhoto(User user);
        Task SignIn(HttpContext httpContext, User user, bool isPersistent = false);
        Task SignOut(HttpContext httpContext);
        int GetCurrentUserId(HttpContext httpContext);
        User GetCurrentUser(HttpContext httpContext);
        Task<List<vwUser>> GetAllUsers();
        Task<List<vwUser>> GetAllUsersForCombobox();
        Task DeleteUser(string userId);
        Task EditUser(User oldUser, User newUser);
        Task AddConnectSiteLog(string netIp, string userId, string browser, string browserversion, bool isMobile, string aciklama = "");
        Task IncorrectLoginNumberUpdate(string userId, bool isLoginSuccess);
        Task<List<vwConnectSiteLog>> GetAllConnectSiteLogs(DateTime baslangicZamani, DateTime bitisZamani);
        Task<List<vwPageVisitMain>> GetAllvwPageVisits();
        Task AddPageVisitMain(string userId, string urlLink, string moduleLink, string? formLink, string? urlId, string? tabId);
        Task AddPageExitMain(string userId, string urlLink, string moduleLink, string? formLink, string? urlId, string? tabId);
        Task AddUserHeartBeat(string userId, string urlLink, string moduleLink, string? formLink, string? urlId, string? tabId);
    }
}
