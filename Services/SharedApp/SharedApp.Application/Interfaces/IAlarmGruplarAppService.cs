using SharedApp.Application.ViewModels;
using SharedKernel.Core;

namespace SharedApp.Application.Interfaces
{
    public interface IAlarmGruplarAppService : IAppService<AlarmGruplarViewModel>
    {
        Task SetSilindi(int id);
    }
}
