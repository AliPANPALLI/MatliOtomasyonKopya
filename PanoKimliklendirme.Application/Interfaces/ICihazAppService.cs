using PanoKimliklendirme.Application.ViewModels;
using SharedKernel.Core;

namespace PanoKimliklendirme.Application.Interfaces
{
    public interface ICihazAppService : IAppService<CihazViewModel>
    {
        Task SetSilindi(int id, string userId);
        Task EditCihaz(CihazViewModel viewModel, string userId);
    }
}