using PanelIdentification.Application.ViewModels;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace PanelIdentification.Application.Interfaces
{
    public interface ICihazAppService : IAppService<CihazViewModel>
    {
        Task SetSilindi(int id, string userId);
        Task EditCihaz(CihazViewModel viewModel, string userId);
    }
}