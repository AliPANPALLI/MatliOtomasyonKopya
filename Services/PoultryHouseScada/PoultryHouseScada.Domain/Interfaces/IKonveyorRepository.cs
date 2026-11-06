using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Domain.Models.ReportModels;
using SharedKernel.Core;

namespace PoultryHouseScada.Domain.Interfaces
{
    public interface IKonveyorRepository : IRepository<Konveyor>
    {
        Task<List<AlarmLog>> GetAlarmLogForKonveyor(int konveyorId);
        Task<List<AlarmLog>> GetAktifAlarmLogForKonveyor(int konveyorId);
        Task<List<AlarmLog>> GetPasifAlarmLogForKonveyor(int konveyorId);
        Task<List<KonveyorZamanCizelgesi>> GetZamanCizelgesiFromSecilenAsync(List<int> secilenKonveyorIdler, DateTime baslangicZamani, DateTime bitisZamani);
        Task<List<KonveyorZamanCizelgesi>> GetKonveyorCalismaDurumuRaporu(int konveyorLokasyonId, DateTime baslangicZamani, DateTime bitisZamani);
        Task<List<KonveyorZamanCizelgesi>> GetKonveyorArizaDurumuRaporu(int konveyorLokasyonId, DateTime baslangicZamani, DateTime bitisZamani);
        Task<List<KonveyorZamanCizelgesi>> GetKonveyorKopmaRaporu(int konveyorLokasyonId, DateTime baslangicZamani, DateTime bitisZamani);
        Task<List<KonveyorZamanListesi>> GetZamanListesiFromSecilenAsync(List<int> secilenKonveyorIdler, DateTime baslangicZamani, DateTime bitisZamani);
        Task<int?> GetKonveyorCizimIdByLokasyonId(int lokasyonId);
    }
}
