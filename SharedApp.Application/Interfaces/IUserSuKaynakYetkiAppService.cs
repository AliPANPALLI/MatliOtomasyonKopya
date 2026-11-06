using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.Models;
using SharedKernel.Core;

namespace SharedApp.Application.Interfaces
{
    public interface IUserSuKaynakYetkiAppService : IAppService<UserSuKaynakYetkiViewModel>
    {
        Task<List<UserSuKaynakYetki>> GetAllUserSuKaynakYetkiByUserId(string userId);
        Task<List<UserSuKaynakYetkiViewModel>> GetAllUserSuKaynakYetkis();
        Task SetSilindi(int id);
        Task AddUserSuKaynakYetki(List<string> secilenKullanicilar, List<int> secilenCizimler, List<int> secilenLokasyonlar, List<int> secilenSirketler, bool okumaYetkisi, bool yazmaYetkisi, bool silindi, bool aktif);
        Task AddUserSuKaynakYetki2(List<string> secilenKullanicilar, List<int> secilenSuKaynaklar, List<int> secilenLokasyonlar, List<int> secilenSirketler, bool okumaYetkisi, bool yazmaYetkisi, bool silindi, bool aktif);
    }
}