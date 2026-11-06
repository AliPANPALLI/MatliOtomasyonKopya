using SharedApp.Application.ViewModels;
using SharedApp.Domain.Models;
using SharedKernel.Core;

namespace SharedApp.Application.Interfaces
{
    public interface ISonGelistirmelerAppService : IAppService<SonGelistirmelerViewModel>
    {
        Task<List<SonGelistirmeler>> GetSonGelistirmeler();
    }
}
