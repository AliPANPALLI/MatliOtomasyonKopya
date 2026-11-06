using EnergyMonitoring.Application.Interfaces;
using EnergyMonitoring.Application.Services.Core;
using EnergyMonitoring.Application.ViewModels;
using EnergyMonitoring.Domain.Interfaces;
using EnergyMonitoring.Domain.Models;
using MapsterMapper;

namespace EnergyMonitoring.Application.Services
{
    public class GatewayAppService : AppService<Gateway, GatewayViewModel>, IGatewayAppService
    {
        private readonly IGatewayRepository _gatewayRepository;
        public GatewayAppService(IMapper mapper, IGatewayRepository gatewayRepository) : base(mapper, gatewayRepository)
        {
            _gatewayRepository = gatewayRepository;
        }

        public async Task AddGateway(GatewayViewModel gatewayViewModel)
        {
            var model = _mapper.Map<Gateway>(gatewayViewModel);
            await _gatewayRepository.AddGateway(model);
        }

        public async Task EditGateway(GatewayViewModel gatewayViewModel)
        {
            var model = _mapper.Map<Gateway>(gatewayViewModel);
            await _gatewayRepository.UpdateAsync(model);
            await _gatewayRepository.SaveChangesAsync();
        }

        public async Task SetSilindi(int id)
        {
            try
            {
                await _gatewayRepository.SetSilindi(id);
            }
            catch (Exception)
            {
            }
        }
    }
}