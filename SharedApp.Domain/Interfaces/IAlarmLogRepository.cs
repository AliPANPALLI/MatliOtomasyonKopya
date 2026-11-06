using SharedApp.Domain.Models;
using SharedKernel.Core;

namespace SharedApp.Domain.Interfaces
{
    public interface IAlarmLogRepository : IRepository<AlarmLog>
    {
        Task<Alarmlar?> GetByAlarmEslemeSozcuguAsync(string alarmEslemeSozcugu, CancellationToken cancellationToken = default);
        Task<int> AddOrUpdateAlarmLogAsync(Alarmlar alarm, CancellationToken cancellationToken = default);
        Task AddYanginAlgilamaLogAsync(YanginAlgilamaLogs log, CancellationToken cancellationToken = default);
    }
}
