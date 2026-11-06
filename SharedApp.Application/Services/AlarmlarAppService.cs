using MapsterMapper;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;

namespace SharedApp.Application.Services
{
    public class AlarmlarAppService : AppService<Alarmlar, AlarmlarViewModel>, IAlarmlarAppService
    {
        private readonly IAlarmlarRepository _alarmlarRepository;
        private readonly IAlarmGrupAlarmEslemeRepository _alarmGrupAlarmEslemeRepository;

        public AlarmlarAppService(IMapper mapper, IAlarmlarRepository alarmlarRepository, IAlarmGrupAlarmEslemeRepository alarmGrupAlarmEslemeRepository) : base(mapper, alarmlarRepository)
        {
            _alarmlarRepository = alarmlarRepository;
            _alarmGrupAlarmEslemeRepository = alarmGrupAlarmEslemeRepository;
        }

        public async Task AddAlarmToAlarmGrup(int alarmId, int alarmGrupId)
        {
            var alarmGrupAlarmEsleme = await _alarmGrupAlarmEslemeRepository.GetAsyncByPredicate(x => x.AlarmId == alarmId && x.AlarmGrupId == alarmGrupId);
            if (alarmGrupAlarmEsleme == null)
            {
                var newAlarmGrupAlarmEsleme = new AlarmGrupAlarmEsleme
                {
                    AlarmId = alarmId,
                    AlarmGrupId = alarmGrupId,
                    Silindi = false
                };
                await _alarmGrupAlarmEslemeRepository.AddAsync(newAlarmGrupAlarmEsleme);
            }
            else
            {
                if (alarmGrupAlarmEsleme.Silindi.GetValueOrDefault(false))
                {
                    alarmGrupAlarmEsleme.Silindi = false;
                }
            }
            await _alarmGrupAlarmEslemeRepository.SaveChangesAsync();
        }

        public async Task<List<Alarmlar>> GetAllAlarmlarByAlarmGrupId(int alarmGrupId)
        {
            try
            {
                var alarmGrupAlarmEslemeler = await _alarmGrupAlarmEslemeRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.AlarmGrupId == alarmGrupId && x.Silindi != true);
                var alarmlar = await _alarmlarRepository.GetAllAsyncByPredicateWithAsNoTracking(x => alarmGrupAlarmEslemeler.Select(y => y.AlarmId).ToList().Contains(x.Id) && x.Silindi != true);
                return alarmlar;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task RemoveAlarmFromAlarmGrup(int alarmId, int alarmGrupId)
        {
            var alarmGrupAlarmEsleme = await _alarmGrupAlarmEslemeRepository.GetAsyncByPredicate(x => x.AlarmId == alarmId && x.AlarmGrupId == alarmGrupId);
            if (alarmGrupAlarmEsleme != null)
            {
                if (!alarmGrupAlarmEsleme.Silindi.GetValueOrDefault(false))
                {
                    alarmGrupAlarmEsleme.Silindi = true;
                }
                await _alarmGrupAlarmEslemeRepository.SaveChangesAsync();
            }
        }

        public async Task SetSilindi(int id)
        {
            var alarm = await _alarmlarRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
            alarm.Silindi = true;
            await _alarmlarRepository.SaveChangesAsync();
        }

        public async Task<List<vwAlarmlar>> GetAllvwAlarmlarByAlarmGrupIdDx(int alarmGrupId)
        {
            var alarmlar = await _alarmlarRepository.GetAllvwAlarmlarByAlarmGrupIdDx(alarmGrupId);
            return alarmlar;
        }

        public async Task<List<vwAlarmlar>> GetAllvwAlarmlarDx()
        {
            var alarmlar = await _alarmlarRepository.GetAllvwAlarmlarDx();
            return alarmlar;
        }
    }
}