using EnergyMonitoring.Application.ViewModels;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace EnergyMonitoring.Application.Interfaces
{
    public interface IGatewayAppService : IAppService<GatewayViewModel>
    {
        Task EditGateway(GatewayViewModel gatewayViewModel);
        Task AddGateway(GatewayViewModel gatewayViewModel);
        Task SetSilindi(int id);
    }
}
