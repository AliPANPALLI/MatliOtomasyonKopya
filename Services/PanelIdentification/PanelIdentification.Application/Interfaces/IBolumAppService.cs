using PanelIdentification.Application.ViewModels;
using PanelIdentification.Domain.Models;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace PanelIdentification.Application.Interfaces
{
    public interface IBolumAppService : IAppService<BolumViewModel>
    {
        Task SetSilindi(int id, string userId);
        Task<List<int>> GetLokasyonIdsByBolumIds(List<int> bolumIds);
        Task EditBolum(BolumViewModel bolumViewModel, string userId);
        Task<List<Bolum>> GetAllBolumForYetki();
    }
}