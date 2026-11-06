using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.Models;
using SharedKernel.Core;

namespace SharedApp.Application.Interfaces
{
    public interface IUserFormYetkiAppService : IAppService<UserFormYetkiViewModel>
    {
        Task<List<UserFormYetki>> GetAllUserFormYetkiByUserId(string userId);
        Task SetSilindi(int id);
        Task<List<UserFormYetkiViewModel>> GetAllUserFormYetkis();
        Task AddUserFormYetki(List<string> secilenKullanicilar, List<int> secilenFormlar, bool okumaYetkisi, bool yazmaYetkisi, bool degistirmeYetkisi, bool silmeYetkisi);
        Task EditUserFormYetki(UserFormYetkiViewModel userFormYetkiViewModel);
    }
}