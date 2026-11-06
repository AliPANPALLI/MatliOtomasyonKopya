using EnergyMonitoring.Application.ViewModels;
using SharedKernel.Core;

namespace EnergyMonitoring.Application.Interfaces
{
    public interface ICihazModelAppService : IAppService<CihazModelViewModel>
    {
        Task EditCihazModel(CihazModelViewModel cihazModelViewModel);
        Task AddCihazModel(CihazModelViewModel cihazModelViewModel);
        Task SetSilindi(int id);
    }
}
