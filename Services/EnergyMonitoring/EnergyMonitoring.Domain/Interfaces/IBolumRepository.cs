using EnergyMonitoring.Domain.DbViewModels;
using EnergyMonitoring.Domain.Models;
using SharedKernel.Core;

namespace EnergyMonitoring.Domain.Interfaces
{
    public interface IBolumRepository : IRepository<Bolum>
    {
        Task AddBolum(Bolum bolum);
        Task SetSilindi(int id);
        Task<vwBolum> GetvwBolumById(int id);
    }
}
