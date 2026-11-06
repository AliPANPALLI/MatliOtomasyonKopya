using MapsterMapper;
using EnergyMonitoring.Application.Interfaces;
using EnergyMonitoring.Application.Services.Core;
using EnergyMonitoring.Application.ViewModels;
using EnergyMonitoring.Domain.Interfaces;
using EnergyMonitoring.Domain.Models;

namespace EnergyMonitoring.Application.Services
{
    public class AlarmLogAppService : AppService<AlarmLog, vwAlarmLogViewModel>, IAlarmLogAppService
    {
        private readonly IAlarmLogRepository _alarmLogRepository;
        public AlarmLogAppService(IMapper mapper, IAlarmLogRepository alarmLogRepository) : base(mapper, alarmLogRepository)
        {
            _alarmLogRepository = alarmLogRepository;
        }

        public async Task<List<vwAlarmLogViewModel>> GetAktifAlarmLogForEnerjiIzleme(int lokasyonId)
        {
            List<vwAlarmLogViewModel> result = new List<vwAlarmLogViewModel>();
            var alarmlar = await _alarmLogRepository.GetAktifAlarmLogForEnerjiIzleme(lokasyonId);
            result.AddRange(_mapper.Map<List<vwAlarmLogViewModel>>(alarmlar));
            return result;
        }

        public async Task<List<vwAlarmLogViewModel>> GetPasifAlarmLogForEnerjiIzleme(int lokasyonId)
        {
            List<vwAlarmLogViewModel> result = new List<vwAlarmLogViewModel>();
            var alarmlar = await _alarmLogRepository.GetPasifAlarmLogForEnerjiIzleme(lokasyonId);
            result.AddRange(_mapper.Map<List<vwAlarmLogViewModel>>(alarmlar));
            return result;
        }
    }
}