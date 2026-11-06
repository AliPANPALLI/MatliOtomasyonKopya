using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Models;

namespace SharedApp.Domain.Interfaces
{
    public interface IYetkiRepository
    {
        Task<List<vwUserFormYetki>> GetUserFormYetkiListByUserId(string userId);
        Task<vwUserFormYetki> GetUserFormYetkiByUserIdAndFormLink(string userId, string formLink);
        Task<List<Tuple<List<int>, string>>> GetPanoKimlikYetkiModelForLokasyonIds(string userId);
        Task<List<Tuple<List<int>, string>>> GetKumesScadaYetkiModelForLokasyonIds(string userId);
        Task<List<Tuple<List<int>, string>>> GetSuHatScadaYetkiModelForLokasyonIds(string userId);
        Task<List<Tuple<List<int>, string>>> GetEnerjiIzlemeYetkiModelForLokasyonIds(string userId);
        Task<List<Tuple<List<int>, string>>> GetSunucuScadaYetkiModelForLokasyonIds(string userId);
        Task<List<UserLinkYetki>> GetUserLinkYetkiByUserId(string userId);
        Task<List<vwUserKumesYetki>> GetUserKumesYetkiForSendingMail();
        Task<List<vwUserSuKaynakYetki>> GetUserSuKaynakYetkiForSendingMail();
        Task<List<vwUserMakineYetki>> GetUserMakineYetkiForSendingMail();
        Task<List<vwSuHatCizimSirketForYetki>> GetvwSuHatCizimSirketForYetki();
        Task<List<Tuple<List<int>, string>>> GetEnerjiIzlemeYetkiModelOnaylayabilirForLokasyonIds(string userId);
        Task<UserLinkYetki?> GetUserLinkYetkiByUserIdAndLink(string userId, string link);
    }
}