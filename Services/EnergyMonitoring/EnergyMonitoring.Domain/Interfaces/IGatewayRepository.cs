using EnergyMonitoring.Domain.Models;
using SharedKernel.Core;

namespace EnergyMonitoring.Domain.Interfaces
{
    public interface IGatewayRepository : IRepository<Gateway>
    {
        Task AddGateway(Gateway gateway);
        Task SetSilindi(int id);
    }
}
