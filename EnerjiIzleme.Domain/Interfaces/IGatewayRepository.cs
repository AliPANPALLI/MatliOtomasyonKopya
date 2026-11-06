using EnerjiIzleme.Domain.Models;
using SharedKernel.Core;

namespace EnerjiIzleme.Domain.Interfaces
{
    public interface IGatewayRepository : IRepository<Gateway>
    {
        Task AddGateway(Gateway gateway);
        Task SetSilindi(int id);
    }
}
