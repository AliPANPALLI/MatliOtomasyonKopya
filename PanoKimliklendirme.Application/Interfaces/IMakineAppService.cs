using PanoKimliklendirme.Application.ViewModels;
using PanoKimliklendirme.Domain.Models;
using SharedKernel.Core;

namespace PanoKimliklendirme.Application.Interfaces
{
    public interface IMakineAppService : IAppService<MakineViewModel>
    {
        Task SetSilindi(int id, string userId);
        Task<List<int>> GetLokasyonIdsByMakineIds(List<int> makineIds);
        Task EditMakine(MakineViewModel makineViewModel, string userId);
        Task<List<Makine>> GetAllMakineForYetki();
    }
}
