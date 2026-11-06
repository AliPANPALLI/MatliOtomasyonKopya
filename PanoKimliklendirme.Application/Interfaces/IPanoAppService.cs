using Microsoft.AspNetCore.Http;
using PanoKimliklendirme.Application.ViewModels;
using SharedKernel.Core;

namespace PanoKimliklendirme.Application.Interfaces
{
    public interface IPanoAppService : IAppService<PanoViewModel>
    {
        Task<PanoViewModel> GetPanoByPanoKimlikNo(string panoKimlikNo);
        Task SetSilindi(int id, string userId);
        Task EditPano(PanoViewModel panoViewModel, string userId);
    }
}
