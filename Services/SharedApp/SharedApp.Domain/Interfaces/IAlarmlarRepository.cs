using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace SharedApp.Domain.Interfaces
{
    public interface IAlarmlarRepository : IRepository<Alarmlar>
    {
        Task<List<vwAlarmlar>> GetAllvwAlarmlarByAlarmGrupIdDx(int alarmGrupId);
        Task<List<vwAlarmlar>> GetAllvwAlarmlarDx();
    }
}
