using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Models;
using SharedKernel.Core;

namespace SharedApp.Domain.Interfaces
{
    public interface IAlarmlarRepository : IRepository<Alarmlar>
    {
        Task<List<vwAlarmlar>> GetAllvwAlarmlarByAlarmGrupIdDx(int alarmGrupId);
        Task<List<vwAlarmlar>> GetAllvwAlarmlarDx();
    }
}
