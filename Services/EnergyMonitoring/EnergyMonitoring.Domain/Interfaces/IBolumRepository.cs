using EnergyMonitoring.Domain.DbViewModels;
using EnergyMonitoring.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace EnergyMonitoring.Domain.Interfaces
{
    public interface IBolumRepository : IRepository<Bolum>
    {
        Task AddBolum(Bolum bolum);
        Task SetSilindi(int id);
        Task<vwBolum> GetvwBolumById(int id);
    }
}
