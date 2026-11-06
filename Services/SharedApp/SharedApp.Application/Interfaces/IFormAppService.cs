using SharedApp.Application.ViewModels;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace SharedApp.Application.Interfaces
{
    public interface IFormAppService : IAppService<FormViewModel>
    {
        Task SetSilindi(int id);
        Task<List<FormViewModel>> GetAllForm();
    }
}
