using SharedApp.Application.ViewModels;
using SharedApp.Domain.Models;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace SharedApp.Application.Interfaces
{
    public interface IGenelParametrelerAppService : IAppService<GenelParametrelerViewModel>
    {
        Task<GenelParametreler?> GetGenelParametrelerDegerByName(string genelParemetreName);
    }
}
