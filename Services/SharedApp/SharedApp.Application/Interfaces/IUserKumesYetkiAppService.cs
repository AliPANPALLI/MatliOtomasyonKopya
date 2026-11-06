using SharedApp.Application.ViewModels;
using SharedApp.Domain.Models;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace SharedApp.Application.Interfaces
{
    public interface IUserKumesYetkiAppService : IAppService<UserKumesYetkiViewModel>
    {
        Task<List<UserKumesYetki>> GetAllUserKumesYetkiByUserId(string userId);
        Task<List<UserKumesYetkiViewModel>> GetAllUserKumesYetkis();
        Task SetSilindi(int id);
        Task AddUserKumesYetki(List<string> secilenKullanicilar, List<int> secilenKumesler, List<int> secilenLokasyonlar, List<int> secilenSirketler, bool okumaYetkisi, bool yazmaYetkisi, bool sorumluMu, bool silindi, bool aktif, bool yemRasyonYetki, bool raporRehberYetki);
    }
}