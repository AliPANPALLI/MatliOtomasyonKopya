using EnergyMonitoring.Domain.DbViewModels;
using EnergyMonitoring.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace EnergyMonitoring.Domain.Interfaces
{
    public interface IAlarmLogRepository : IRepository<AlarmLog>
    {
        Task<List<vwAlarmLog>> GetAktifAlarmLogForEnerjiIzleme(int lokasyonId);
        Task<List<vwAlarmLog>> GetPasifAlarmLogForEnerjiIzleme(int lokasyonId);
    }
}
