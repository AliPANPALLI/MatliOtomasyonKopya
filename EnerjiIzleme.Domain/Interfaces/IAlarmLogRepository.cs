using EnerjiIzleme.Domain.DbViewModels;
using EnerjiIzleme.Domain.Models;
using SharedKernel.Core;

namespace EnerjiIzleme.Domain.Interfaces
{
    public interface IAlarmLogRepository : IRepository<AlarmLog>
    {
        Task<List<vwAlarmLog>> GetAktifAlarmLogForEnerjiIzleme(int lokasyonId);
        Task<List<vwAlarmLog>> GetPasifAlarmLogForEnerjiIzleme(int lokasyonId);
    }
}
