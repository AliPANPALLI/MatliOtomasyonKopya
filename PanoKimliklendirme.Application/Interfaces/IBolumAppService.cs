using PanoKimliklendirme.Application.ViewModels;
using PanoKimliklendirme.Domain.Models;
using SharedKernel.Core;

namespace PanoKimliklendirme.Application.Interfaces
{
    public interface IBolumAppService : IAppService<BolumViewModel>
    {
        Task SetSilindi(int id, string userId);
        Task<List<int>> GetLokasyonIdsByBolumIds(List<int> bolumIds);
        Task EditBolum(BolumViewModel bolumViewModel, string userId);
        Task<List<Bolum>> GetAllBolumForYetki();
    }
}