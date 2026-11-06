using EnerjiIzleme.Application.ViewModels;
using SharedKernel.Core;

namespace EnerjiIzleme.Application.Interfaces
{
    public interface IAlarmLogAppService : IAppService<vwAlarmLogViewModel>
    {
        Task<List<vwAlarmLogViewModel>> GetAktifAlarmLogForEnerjiIzleme(int lokasyonId);
        Task<List<vwAlarmLogViewModel>> GetPasifAlarmLogForEnerjiIzleme(int lokasyonId);
    }
}
