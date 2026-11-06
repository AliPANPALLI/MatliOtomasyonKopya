using SharedApp.Application.ViewModels;
using SharedApp.Domain.Models;
using SharedKernel.Core;

namespace SharedApp.Application.Interfaces
{
    public interface IGenelParametrelerAppService : IAppService<GenelParametrelerViewModel>
    {
        Task<GenelParametreler?> GetGenelParametrelerDegerByName(string genelParemetreName);
    }
}
