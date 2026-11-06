using EnerjiIzleme.Application.ViewModels;
using SharedKernel.Core;

namespace EnerjiIzleme.Application.Interfaces
{
    public interface ICihazModelAppService : IAppService<CihazModelViewModel>
    {
        Task EditCihazModel(CihazModelViewModel cihazModelViewModel);
        Task AddCihazModel(CihazModelViewModel cihazModelViewModel);
        Task SetSilindi(int id);
    }
}
