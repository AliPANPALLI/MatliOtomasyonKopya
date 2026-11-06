using SharedApp.Application.ViewModels;
using SharedApp.Domain.Interfaces;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace SharedApp.Application.Interfaces
{
    public interface IUserYetkiAppService : IAppService<UserYetkiViewModel>
    {
        Task AddUserYetki(List<string> secilenKullanicilar, List<string> secilenLinks, int formId, bool okumaYetkisi, bool yazmaYetkisi, bool degistirmeYetkisi, bool silmeYetkisi, bool onaylayabilirMi, bool sorumluMu, bool silindi, bool aktif);
        Task SetSilindi(int id);
        Task<List<UserYetkiViewModel>> GetAllByUserId(string userId);
        Task<List<UserYetkiViewModel>> GetKantarYetkiByUserId(string userId);
        Task<List<UserYetkiViewModel>> GetAllByFormId(int formId);
        Task<List<UserYetkiDetayModel>> GetAllByModulId(int modulId);
        Task<List<UserYetkiDetayModel>> GetAllEnriched();
        Task<int> TransferUserYetkiByModul(string kaynakUserId, List<string> hedefUserIdler, int modulId);
    }
}
