using SharedApp.Application.ViewModels;
using SharedApp.Domain.Models;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace SharedApp.Application.Interfaces
{
    public interface ISonGelistirmelerAppService : IAppService<SonGelistirmelerViewModel>
    {
        Task<List<SonGelistirmeler>> GetSonGelistirmeler();
    }
}
