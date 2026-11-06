using SharedApp.Application.ViewModels;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace SharedApp.Application.Interfaces
{
    public interface IAlarmGruplarAppService : IAppService<AlarmGruplarViewModel>
    {
        Task SetSilindi(int id);
    }
}
