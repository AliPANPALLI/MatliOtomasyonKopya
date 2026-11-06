using EnerjiIzleme.Domain.DbViewModels;
using EnerjiIzleme.Domain.Models;
using SharedKernel.Core;

namespace EnerjiIzleme.Domain.Interfaces
{
    public interface IMakineRepository : IRepository<Makine>
    {
        Task AddMakine(Makine makine);

        Task SetSilindi(int id);
        Task<vwMakine> GetvwMakineById(int id);
    }
}
