using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Models;
using SharedKernel.Core;

namespace SharedApp.Application.Interfaces
{
    public interface IUserLinkYetkiAppService : IAppService<UserLinkYetkiViewModel>
    {
        Task<List<vwUserLinkYetki>> GetAllUserLinkYetkiForTesisScada();
        Task<List<vwUserLinkYetki>> GetAllUserLinkYetkiForSunucuScada();
        Task SetSilindi(int id);
        Task AddUserLinkYetki(List<string> secilenKullanicilar, List<string> secilenLinks, int formId, bool okumaYetkisi, bool yazmaYetkisi, bool sorumluMu, bool silindi, bool aktif);
        Task<List<Form>> GetAllFormForTesisScada();
        Task<List<Form>> GetAllFormForSunucuScada();
        Task EditUserLinkYetki(UserLinkYetkiViewModel userLinkYetkiViewModel);
    }
}
