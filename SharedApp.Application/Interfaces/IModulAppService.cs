using SharedApp.Application.ViewModels;
using SharedKernel.Core;

namespace SharedApp.Application.Interfaces
{
    public interface IModulAppService : IAppService<ModulViewModel>
    {
        Task SetSilindi(int id);
    }
}
