using SharedApp.Application.ViewModels;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace SharedApp.Application.Interfaces
{
    public interface IModulAppService : IAppService<ModulViewModel>
    {
        Task SetSilindi(int id);
    }
}
