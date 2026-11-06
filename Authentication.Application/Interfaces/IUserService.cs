using Authentication.Application.ViewModels;
using Authentication.Domain.DbViewModels;
using Authentication.Domain.Models;
using Authentication.Domain.Models.ReportModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.Application.Interfaces
{
    public interface IUserService
    {
        Task<User> GetUserByName(string userName);
        Task<User> GetUserBySicilNo(string sicilNo);
        Task<User> GetUserById(string userId);
        Task<UserViewModel> CreateUser(UserViewModel userViewModel);
        Task EditUser(UserViewModel userViewModel);
        Task DeleteUser(string userId);
        Task<List<vwUser>> GetAllUsers();
        Task<List<vwUser>> GetAllUsersForCombobox();
        Task<List<ConnectSiteLogReportModel>> GetAllUserConnectSiteLogs(DateTime baslangicZamani, DateTime bitisZamani);
        Task<List<PageVisitReportModel>> GetAllvwPageVisits();
        Task<bool> CheckPasswordAsync(User user, string password);
        Task ChangePassword(User user, string password);
        Task<string> ResetPassword(User user);
        Task<bool> ChangeProfilePhoto(User user, IFormFile? formFile);
        Task<bool> DeleteProfilePhoto(User user);
        Task AddConnectSiteLog(string netIp, string userId, string browser, string browserversion, bool isMobile, string aciklama = "");
        Task IncorrectLoginNumberUpdate(string userId, bool isLoginSuccess);
        Task<string> PostSms();
        Task AddPageVisitMain(string userId, string urlLink, string moduleLink, string? formLink, string? urlId, string? tabId);
        Task AddPageExitMain(string userId, string urlLink, string moduleLink, string? formLink, string? urlId, string? tabId);
        Task AddUserHeartBeat(string userId, string urlLink, string moduleLink, string? formLink, string? urlId, string? tabId);
    }
}