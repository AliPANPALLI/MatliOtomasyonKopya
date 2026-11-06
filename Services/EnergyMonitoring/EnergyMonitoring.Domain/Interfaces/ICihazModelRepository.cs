using EnergyMonitoring.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace EnergyMonitoring.Domain.Interfaces
{
    public interface ICihazModelRepository : IRepository<CihazModel>
    {
        Task AddCihazModel(CihazModel cihazModel);
        Task SetSilindi(int id);
    }
}