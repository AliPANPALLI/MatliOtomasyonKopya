using SharedApp.Application.ViewModels;
using SharedApp.Domain.Models;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace SharedApp.Application.Interfaces
{
    public interface IUserEnerjiIzlemeYetkiAppService : IAppService<UserEnerjiIzlemeYetkiViewModel>
    {
        Task<List<UserEnerjiIzlemeYetki>> GetAllUserEnerjiIzlemeYetkiByUserId(string userId);
        Task<List<UserEnerjiIzlemeYetkiViewModel>> GetAllUserEnerjiIzlemeYetkis();
        Task SetSilindi(int id);
        Task AddUserEnerjiIzlemeYetki(List<string> secilenKullanicilar,List<int> secilenLokasyonlar, List<int> secilenSirketler, bool okumaYetkisi, bool yazmaYetkisi, bool sorumluMu, bool silindi, bool aktif);
    }
}