using EnergyMonitoring.Application.ViewModels;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace EnergyMonitoring.Application.Interfaces
{
    public interface IAlarmLogAppService : IAppService<vwAlarmLogViewModel>
    {
        Task<List<vwAlarmLogViewModel>> GetAktifAlarmLogForEnerjiIzleme(int lokasyonId);
        Task<List<vwAlarmLogViewModel>> GetPasifAlarmLogForEnerjiIzleme(int lokasyonId);
    }
}
