using SharedApp.Application.ViewModels;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Models;
using static SharedKernel.ViewModels.UserYetkiClaimViewModel;

namespace SharedApp.Application.Interfaces
{
    public interface IYetkiAppService
    {
        Task<List<vwUserFormYetki>> GetUserFormYetkiListByUserId(string userId);
        Task<vwUserFormYetki> GetUserFormYetkiByUserIdAndFormLink(string userId, string formLink);
        Task<List<PermissionDetail>> GetPermissionDetailForPanoKimliklendirme(string userId);
        Task<List<PermissionDetail>> GetPermissionDetailForKumesScada(string userId);
        Task<List<PermissionDetail>> GetPermissionDetailForSuHatScada(string userId);
        Task<List<PermissionDetail>> GetPermissionDetailForEnerjiIzleme(string userId);
        Task<List<PermissionDetail>> GetPermissionDetailForSunucuScada(string userId);
        Task<List<PermissionDetail>> GetPermissionDetailForTesisScada(string userId);
        Task<List<PermissionDetail>> GetPermissionDetailForTopoloji(string userId);
        Task<List<PermissionDetail>> GetPermissionDetailForTicketAndYonetim(string userId);
        //Task<PermissionDetail> GetPermissionDetailForPanoKimliklendirme2(string userId);
        //Task<PermissionDetail> GetPermissionDetailForKumesScada2(string userId);
        Task<List<MenuItem>> GetAllSideBar();
        Task<List<MenuItem>> GetAllTopBar();
        Task<List<string>> SendEmailForScheduledMaintenance(bool panoKimlikKullanicisiMi, bool kumesScadaKullanicisiMi, bool suHatScadaKullanicisiMi);
        Task<bool> GetvwUserEnerjiIzlemeYetkiByUserIdAndLokasyonId(string userId, int lokasyonId);
        Task<UserLinkYetki?> GetUserLinkYetkiByUserIdAndLink(string userId, string link);
    }
}