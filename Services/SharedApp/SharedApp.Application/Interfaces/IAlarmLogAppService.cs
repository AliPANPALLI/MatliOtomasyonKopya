using SharedApp.Application.ViewModels;
using SharedApp.Application.ViewModels.RequestModel;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace SharedApp.Application.Interfaces
{
    public interface IAlarmLogAppService : IAppService<AlarmLogViewModel>
    {
        Task<KameraAlarmResult> AlarmBildirAsync(KameraAlarmRequest request, CancellationToken cancellationToken = default);
    }
}
