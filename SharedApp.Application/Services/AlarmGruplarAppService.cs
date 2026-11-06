using MapsterMapper;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;

namespace SharedApp.Application.Services
{
    public class AlarmGruplarAppService : AppService<AlarmGruplar, AlarmGruplarViewModel>, IAlarmGruplarAppService
    {
        private readonly IAlarmGruplarRepository _alarmGruplarRepository;

        public AlarmGruplarAppService(IMapper mapper, IAlarmGruplarRepository alarmGruplarRepository) : base(mapper, alarmGruplarRepository)
        {
            _alarmGruplarRepository = alarmGruplarRepository;
        }

        public async Task SetSilindi(int id)
        {
            var alarmGrup = await _alarmGruplarRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
            alarmGrup.Silindi = true;
            await _alarmGruplarRepository.SaveChangesAsync();
        }
    }
}
