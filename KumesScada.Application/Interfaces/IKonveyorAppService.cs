using KumesScada.Application.ViewModels;
using KumesScada.Domain.Models.ReportModels;
using SharedKernel.Core;

namespace KumesScada.Application.Interfaces
{
    public interface IKonveyorAppService : IAppService<KonveyorViewModel>
    {
        Task<int?> GetKonveyorCizimIdByLokasyonId(int lokasyonId);
        Task<List<AlarmViewModel>> GetAlarmLogForKonveyor(int konveyorId);
        Task<List<AlarmViewModel>> GetAktifAlarmLogForKonveyor(int konveyorId);
        Task<List<AlarmViewModel>> GetPasifAlarmLogForKonveyor(int konveyorId);
        Task<List<KonveyorZamanCizelgesi>> GetZamanCizelgesiFromSecilenAsync(List<int> secilenKonveyorIdler, DateTime baslangicZamani, DateTime bitisZamani);
        Task<List<KonveyorZamanCizelgesi>> GetKonveyorCalismaDurumuRaporu(int konveyorLokasyonId, DateTime baslangicZamani, DateTime bitisZamani);
        Task<List<KonveyorZamanCizelgesi>> GetKonveyorArizaDurumuRaporu(int konveyorLokasyonId, DateTime baslangicZamani, DateTime bitisZamani);
        Task<List<KonveyorZamanCizelgesi>> GetKonveyorKopmaRaporu(int konveyorLokasyonId, DateTime baslangicZamani, DateTime bitisZamani);
        Task<List<KonveyorZamanListesi>> GetZamanListesiFromSecilenAsync(List<int> secilenKonveyorIdler, DateTime baslangicZamani, DateTime bitisZamani);
    }
}