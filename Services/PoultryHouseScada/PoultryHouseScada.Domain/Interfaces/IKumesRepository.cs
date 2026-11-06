using PoultryHouseScada.Domain.DbViewModels;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Domain.Models.ReportModels;
using SharedKernel.Core;

namespace PoultryHouseScada.Domain.Interfaces
{
    public interface IKumesRepository : IRepository<Kumes>
    {
        Task<List<AlarmLog>> GetAlarmLogForKumes(int kumesId);
        Task<List<AlarmLog>> GetAktifAlarmLogForKumes(int kumesId);
        Task<List<AlarmLog>> GetPasifAlarmLogForKumes(int kumesId);
        Task<List<HayvanCins>> GetAllHayvanCins();
        Task<bool> SaveTesisYemRasyonAyarlariByLokasyonId(TesisYemRasyonAyarModel tesisYemRasyonAyarModel, CancellationToken cancellationToken);
        Task<TesisYemRasyonAyarModel?> GetTesisYemRasyonAyarlariByLokasyonId(int lokasyonId, CancellationToken cancellationToken);
        Task<vwLokasyon> GetLokasyonById(int lokasyonId);
        Task<List<vwLokasyon>> GetAllLokasyonBySirketId(int sirketId);
        Task<List<vwUserKumesYetkis>> GetAllUserKumesYetkiByUserId(string userId);
        Task<List<PlcBaglantiKesintiGunlukRaporModel>> GetPlcBaglantiKesintiGunlukRaporuByKumesIds(List<int> kumesIds, DateTime baslangicZamani, DateTime bitisZamani);
        Task<List<PlcBaglantiKesintiLog>> GetPlcBaglantiKesintiDetayByKumesId(int kumesId, DateTime baslangicZamani, DateTime bitisZamani);
    }
}