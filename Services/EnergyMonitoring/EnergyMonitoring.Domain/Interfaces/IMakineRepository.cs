using EnergyMonitoring.Domain.DbViewModels;
using EnergyMonitoring.Domain.Models;
using SharedKernel.Core;

namespace EnergyMonitoring.Domain.Interfaces
{
    public interface IMakineRepository : IRepository<Makine>
    {
        Task AddMakine(Makine makine);

        Task SetSilindi(int id);
        Task<vwMakine> GetvwMakineById(int id);
    }
}
