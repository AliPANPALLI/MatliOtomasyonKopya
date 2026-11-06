using Authentication.Application.ViewModels;
using Authentication.Domain.DbViewModels;
using Authentication.Domain.Models;
using Authentication.Domain.Models.ReportModels;
using Microsoft.AspNetCore.Http;

namespace Authentication.Application.Interfaces
{
    /// <summary>
    /// Kullanıcı hesabı işlemlerinin uygulama katmanı sözleşmesi: kullanıcı CRUD'u, şifre ve profil
    /// fotoğrafı işlemleri, giriş kayıtları ve sayfa ziyaret/heartbeat kayıtları.
    /// </summary>
    public interface IUserService
    {
        /// <summary>Kullanıcı adına göre kullanıcıyı döner; bulunamazsa null.</summary>
        Task<User> GetUserByName(string userName);

        /// <summary>Sicil numarasına göre kullanıcıyı döner; giriş ve şifre sıfırlamada kullanılır. Bulunamazsa null.</summary>
        Task<User> GetUserBySicilNo(string sicilNo);

        /// <summary>Id'ye göre kullanıcıyı döner; bulunamazsa hata fırlatır.</summary>
        Task<User> GetUserById(string userId);

        /// <summary>Yeni kullanıcı oluşturur; aynı kullanıcı adı varsa hata fırlatır.</summary>
        Task<UserViewModel> CreateUser(UserViewModel userViewModel);

        /// <summary>Mevcut kullanıcının bilgilerini günceller.</summary>
        Task EditUser(UserViewModel userViewModel);

        /// <summary>Kullanıcıyı siler.</summary>
        Task DeleteUser(string userId);

        /// <summary>Tüm kullanıcıları lokasyon ve şirket bilgileriyle birlikte döner.</summary>
        Task<List<vwUser>> GetAllUsers();

        /// <summary>Seçim kutularında gösterilecek kullanıcı listesini döner.</summary>
        Task<List<vwUser>> GetAllUsersForCombobox();

        /// <summary>Tarih aralığındaki giriş/çıkış kayıtlarını oturumlar halinde eşleştirip döner.</summary>
        Task<List<ConnectSiteLogReportModel>> GetAllUserConnectSiteLogs(DateTime baslangicZamani, DateTime bitisZamani);

        /// <summary>Sayfa ziyaret raporunu döner.</summary>
        Task<List<PageVisitReportModel>> GetAllvwPageVisits();

        /// <summary>Verilen şifrenin kullanıcının şifresiyle eşleşip eşleşmediğini kontrol eder.</summary>
        Task<bool> CheckPasswordAsync(User user, string password);

        /// <summary>Kullanıcının şifresini verilen yeni şifreyle değiştirir.</summary>
        Task ChangePassword(User user, string password);

        /// <summary>Kullanıcıya rastgele yeni bir şifre atar ve bu şifreyi döner.</summary>
        Task<string> ResetPassword(User user);

        /// <summary>Kullanıcının profil fotoğrafını kaydeder veya değiştirir.</summary>
        Task<bool> ChangeProfilePhoto(User user, IFormFile? formFile);

        /// <summary>Kullanıcının profil fotoğrafını siler.</summary>
        Task<bool> DeleteProfilePhoto(User user);

        /// <summary>Giriş, çıkış veya başarısız giriş denemesini ConnectSiteLog'a yazar.</summary>
        Task AddConnectSiteLog(string netIp, string userId, string browser, string browserversion, bool isMobile, string aciklama = "");

        /// <summary>Başarısız giriş sayacını günceller: başarılı girişte sıfırlar, başarısız denemede artırır.</summary>
        Task IncorrectLoginNumberUpdate(string userId, bool isLoginSuccess);

        /// <summary>Kullanıcının bir sayfaya girişini kaydeder.</summary>
        Task AddPageVisitMain(string userId, string urlLink, string moduleLink, string? formLink, string? urlId, string? tabId);

        /// <summary>Kullanıcının bir sayfadan (sekme kapanışıyla) çıkışını kaydeder.</summary>
        Task AddPageExitMain(string userId, string urlLink, string moduleLink, string? formLink, string? urlId, string? tabId);

        /// <summary>Açık sekmeden gelen heartbeat'i kaydeder.</summary>
        Task AddUserHeartBeat(string userId, string urlLink, string moduleLink, string? formLink, string? urlId, string? tabId);
    }
}
