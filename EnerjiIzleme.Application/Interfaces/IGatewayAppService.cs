using EnerjiIzleme.Application.ViewModels;
using SharedKernel.Core;

namespace EnerjiIzleme.Application.Interfaces
{
    public interface IGatewayAppService : IAppService<GatewayViewModel>
    {
        Task EditGateway(GatewayViewModel gatewayViewModel);
        Task AddGateway(GatewayViewModel gatewayViewModel);
        Task SetSilindi(int id);
    }
}
