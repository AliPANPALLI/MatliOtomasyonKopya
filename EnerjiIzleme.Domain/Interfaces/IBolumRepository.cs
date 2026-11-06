using EnerjiIzleme.Domain.DbViewModels;
using EnerjiIzleme.Domain.Models;
using SharedKernel.Core;

namespace EnerjiIzleme.Domain.Interfaces
{
    public interface IBolumRepository : IRepository<Bolum>
    {
        Task AddBolum(Bolum bolum);
        Task SetSilindi(int id);
        Task<vwBolum> GetvwBolumById(int id);
    }
}
