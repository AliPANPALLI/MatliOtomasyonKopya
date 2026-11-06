using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace SharedApp.Domain.Interfaces
{
    public interface IUserLinkYetkiRepository : IRepository<UserLinkYetki>
    {
        Task AddUserLinkYetki(List<string> secilenKullanicilar, List<string> secilenLinks,int formId, bool okumaYetkisi, bool yazmaYetkisi, bool sorumluMu, bool silindi, bool aktif);
        Task<List<vwUserLinkYetki>> GetAllUserLinkYetkiForTesisScada();
        Task<List<vwUserLinkYetki>> GetAllUserLinkYetkiForSunucuScada();
    }
}
