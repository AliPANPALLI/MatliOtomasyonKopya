using EnergyMonitoring.Application.ViewModels;
using EnergyMonitoring.Domain.Models;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace EnergyMonitoring.Application.Interfaces
{
    public interface IBolumAppService : IAppService<BolumViewModel>
    {
        Task SetSilindi(int id);
        Task EditBolum(BolumViewModel bolumViewModel, string userId);
        Task AddBolum(BolumViewModel bolumViewModel, string userId);
        Task<List<Bolum>> GetAllBolumByLokasyonId(int lokasyonId);
        Task<BolumViewModel> GetvwBolumById(int id);
        Task<bool> OnaylaBolum(int id, string userId);
    }
}
