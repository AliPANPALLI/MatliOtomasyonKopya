using Microsoft.AspNetCore.Http;
using PanelIdentification.Application.ViewModels;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace PanelIdentification.Application.Interfaces
{
    public interface IPanoAppService : IAppService<PanoViewModel>
    {
        Task<PanoViewModel> GetPanoByPanoKimlikNo(string panoKimlikNo);
        Task SetSilindi(int id, string userId);
        Task EditPano(PanoViewModel panoViewModel, string userId);
    }
}
