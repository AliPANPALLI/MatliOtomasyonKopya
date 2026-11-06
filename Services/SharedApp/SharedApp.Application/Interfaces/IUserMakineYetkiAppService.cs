using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.Models;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace SharedApp.Application.Interfaces
{
    public interface IUserMakineYetkiAppService : IAppService<UserMakineYetkiViewModel>
    {
        Task<List<UserMakineYetki>> GetAllUserMakineYetkiByUserId(string userId);
        Task<List<UserMakineYetkiViewModel>> GetAllUserMakineYetkis(string userId);
        Task SetSilindi(int id);
        Task AddUserMakineYetki(List<string> secilenKullanicilar, List<int> secilenMakineler, List<int> secilenBolumler, List<int> secilenLokasyonlar, List<int> secilenSirketler, bool okumaYetkisi, bool yazmaYetkisi, bool sorumluMu, bool silindi, bool aktif);
        Task EditUserMakineYetki(UserMakineYetkiViewModel userMakineYetkiViewModel);
    }
}
