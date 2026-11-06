using MapsterMapper;
using PoultryHouseScada.Application.Interfaces;
using PoultryHouseScada.Application.ViewModels;
using PoultryHouseScada.Domain.Interfaces;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Domain.Models.ReportModels;
using PanelIdentification.Application.Services.Core;

namespace PoultryHouseScada.Application.Services
{
    public class KonveyorAppService : AppService<Konveyor, KonveyorViewModel>, IKonveyorAppService
    {
        private readonly IKonveyorRepository _konveyorRepository;
        private readonly IAlarmLogRepository _alarmLogRepository;
        private readonly IEtiketLogTempRepository _etiketLogTempRepository;

        public KonveyorAppService(IMapper mapper, IKonveyorRepository konveyorRepository, IAlarmLogRepository alarmLogRepository, IEtiketLogTempRepository etiketLogTempRepository) : base(mapper, konveyorRepository)
        {
            _konveyorRepository = konveyorRepository;
            _alarmLogRepository = alarmLogRepository;
            _etiketLogTempRepository = etiketLogTempRepository;
        }
        public async Task<List<AlarmViewModel>> GetAlarmLogForKonveyor(int konveyorId)
        {
            List<AlarmViewModel> result = new List<AlarmViewModel>();
            var alarmlar = await _konveyorRepository.GetAlarmLogForKonveyor(konveyorId);

            result.AddRange(_mapper.Map<List<AlarmViewModel>>(alarmlar));
            return result;
        }

        public async Task<List<AlarmViewModel>> GetAktifAlarmLogForKonveyor(int konveyorId)
        {
            List<AlarmViewModel> result = new List<AlarmViewModel>();
            var alarmlar = await _konveyorRepository.GetAktifAlarmLogForKonveyor(konveyorId);

            result.AddRange(_mapper.Map<List<AlarmViewModel>>(alarmlar));
            return result;
        }

        public async Task<List<AlarmViewModel>> GetPasifAlarmLogForKonveyor(int konveyorId)
        {
            List<AlarmViewModel> result = new List<AlarmViewModel>();
            var alarmlar = await _konveyorRepository.GetPasifAlarmLogForKonveyor(konveyorId);

            result.AddRange(_mapper.Map<List<AlarmViewModel>>(alarmlar));
            return result;
        }

        public async Task<List<KonveyorZamanCizelgesi>> GetZamanCizelgesiFromSecilenAsync(List<int> secilenKonveyorIdler, DateTime baslangicZamani, DateTime bitisZamani)
        {
            var result = await _konveyorRepository.GetZamanCizelgesiFromSecilenAsync(secilenKonveyorIdler, baslangicZamani, bitisZamani);
            return result;
        }

        public async Task<List<KonveyorZamanCizelgesi>> GetKonveyorCalismaDurumuRaporu(int konveyorLokasyonId, DateTime baslangicZamani, DateTime bitisZamani)
        {
            var calismaTablosu = await _konveyorRepository.GetKonveyorCalismaDurumuRaporu(konveyorLokasyonId, baslangicZamani, bitisZamani);
            return calismaTablosu;
        }

        public async Task<List<KonveyorZamanCizelgesi>> GetKonveyorArizaDurumuRaporu(int konveyorLokasyonId, DateTime baslangicZamani, DateTime bitisZamani)
        {
            var arizaTablosu = await _konveyorRepository.GetKonveyorArizaDurumuRaporu(konveyorLokasyonId, baslangicZamani, bitisZamani);
            return arizaTablosu;
        }
        public async Task<List<KonveyorZamanCizelgesi>> GetKonveyorKopmaRaporu(int konveyorLokasyonId, DateTime baslangicZamani, DateTime bitisZamani)
        {
            var arizaTablosu = await _konveyorRepository.GetKonveyorKopmaRaporu(konveyorLokasyonId, baslangicZamani, bitisZamani);
            return arizaTablosu;
        }

        public async Task<List<KonveyorZamanListesi>> GetZamanListesiFromSecilenAsync(List<int> secilenKonveyorIdler, DateTime baslangicZamani, DateTime bitisZamani)
        {
            var result = await _konveyorRepository.GetZamanListesiFromSecilenAsync(secilenKonveyorIdler, baslangicZamani, bitisZamani);
            return result;
        }

        public async Task<int?> GetKonveyorCizimIdByLokasyonId(int lokasyonId)
        {
            var result = await _konveyorRepository.GetKonveyorCizimIdByLokasyonId(lokasyonId);
            return result;
        }
    }
}