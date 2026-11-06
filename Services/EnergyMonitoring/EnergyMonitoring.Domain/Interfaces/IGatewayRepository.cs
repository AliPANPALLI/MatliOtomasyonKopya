using EnergyMonitoring.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace EnergyMonitoring.Domain.Interfaces
{
    public interface IGatewayRepository : IRepository<Gateway>
    {
        Task AddGateway(Gateway gateway);
        Task SetSilindi(int id);
    }
}
