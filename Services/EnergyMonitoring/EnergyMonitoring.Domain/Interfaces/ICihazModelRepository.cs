using EnergyMonitoring.Domain.Models;
using SharedKernel.Core;

namespace EnergyMonitoring.Domain.Interfaces
{
    public interface ICihazModelRepository : IRepository<CihazModel>
    {
        Task AddCihazModel(CihazModel cihazModel);
        Task SetSilindi(int id);
    }
}