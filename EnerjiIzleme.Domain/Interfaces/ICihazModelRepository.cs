using EnerjiIzleme.Domain.Models;
using SharedKernel.Core;

namespace EnerjiIzleme.Domain.Interfaces
{
    public interface ICihazModelRepository : IRepository<CihazModel>
    {
        Task AddCihazModel(CihazModel cihazModel);
        Task SetSilindi(int id);
    }
}