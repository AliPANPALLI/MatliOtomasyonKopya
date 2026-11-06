using EnergyMonitoring.Application.Interfaces;
using EnergyMonitoring.Application.Services.Core;
using EnergyMonitoring.Application.ViewModels;
using EnergyMonitoring.Domain.Interfaces;
using EnergyMonitoring.Domain.Models;
using MapsterMapper;

namespace EnergyMonitoring.Application.Services
{
    public class CihazModelAppService : AppService<CihazModel, CihazModelViewModel>, ICihazModelAppService
    {
        private readonly ICihazModelRepository _cihazModelRepository;
        public CihazModelAppService(IMapper mapper, ICihazModelRepository cihazModelRepository) : base(mapper, cihazModelRepository)
        {
            _cihazModelRepository = cihazModelRepository;
        }

        public async Task AddCihazModel(CihazModelViewModel cihazModelViewModel)
        {
            var model = _mapper.Map<CihazModel>(cihazModelViewModel);
            await _cihazModelRepository.AddCihazModel(model);
        }

        public async Task EditCihazModel(CihazModelViewModel cihazModelViewModel)
        {
            var model = _mapper.Map<CihazModel>(cihazModelViewModel);
            await _cihazModelRepository.UpdateAsync(model);
            await _cihazModelRepository.SaveChangesAsync();
        }
        public async Task SetSilindi(int id)
        {
            try
            {
                await _cihazModelRepository.SetSilindi(id);
            }
            catch (Exception)
            {
            }
        }
    }
}