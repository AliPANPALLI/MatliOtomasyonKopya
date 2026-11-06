using MakineIzleme.Application.Interfaces;
using MakineIzleme.Domain.Interfaces;
using MakineIzleme.Domain.Models.ReportModels;
using MapsterMapper;

namespace MakineIzleme.Application.Services
{
    public class MakineIzlemeAppService : IMakineIzlemeAppService
    {
        private readonly IMakineIzlemeRepository _makineIzlemeRepository;

        public MakineIzlemeAppService(IMapper mapper, IMakineIzlemeRepository makineIzlemeRepository)
        {
            _makineIzlemeRepository = makineIzlemeRepository;
        }
        public async Task<List<MachineDashboardDto>> BuildDashboardData(int bolumId, CancellationToken ct)
        {
            return await _makineIzlemeRepository.BuildDashboardData(bolumId, ct);
        }

        public async Task<List<MachineLogRowDto>> GetMachineLogs(int machineId, int take)
        {
            return await _makineIzlemeRepository.GetMachineLogs(machineId, take);
        }
    }
}
