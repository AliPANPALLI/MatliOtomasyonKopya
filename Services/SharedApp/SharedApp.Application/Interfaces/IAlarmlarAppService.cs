using SharedApp.Application.ViewModels;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Models;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace SharedApp.Application.Interfaces
{
    public interface IAlarmlarAppService : IAppService<AlarmlarViewModel>
    {
        Task SetSilindi(int id);
        Task<List<Alarmlar>> GetAllAlarmlarByAlarmGrupId(int alarmGrupId);
        Task<List<vwAlarmlar>> GetAllvwAlarmlarDx();
        Task<List<vwAlarmlar>> GetAllvwAlarmlarByAlarmGrupIdDx(int alarmGrupId);
        Task AddAlarmToAlarmGrup(int alarmId, int alarmGrupId);
        Task RemoveAlarmFromAlarmGrup(int alarmId, int alarmGrupId);

    }
}
