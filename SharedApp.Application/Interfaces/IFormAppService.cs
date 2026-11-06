using SharedApp.Application.ViewModels;
using SharedKernel.Core;

namespace SharedApp.Application.Interfaces
{
    public interface IFormAppService : IAppService<FormViewModel>
    {
        Task SetSilindi(int id);
        Task<List<FormViewModel>> GetAllForm();
    }
}
