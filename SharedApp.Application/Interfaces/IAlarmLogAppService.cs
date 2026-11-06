using SharedApp.Application.ViewModels;
using SharedApp.Application.ViewModels.RequestModel;
using SharedKernel.Core;

namespace SharedApp.Application.Interfaces
{
    public interface IAlarmLogAppService : IAppService<AlarmLogViewModel>
    {
        Task<KameraAlarmResult> AlarmBildirAsync(KameraAlarmRequest request, CancellationToken cancellationToken = default);
    }
}
