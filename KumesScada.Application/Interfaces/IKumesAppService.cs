using KumesScada.Application.ViewModels;
using KumesScada.Domain.Models;
using KumesScada.Domain.Models.ReportModels;
using SharedKernel.Core;

namespace KumesScada.Application.Interfaces
{
    public interface IKumesAppService : IAppService<KumesViewModel>
    {
        Task<List<Kumes>> GetAllKumesForYetki();
        Task<List<int>> GetLokasyonIdsByKumesIds(List<int> kumesIds);
        Task<List<int>> GetKumesIdsByLokasyonIds(List<int> lokasyonIds);
        Task<List<TesisGenelIzlemeModel>> GetTesisGenelIzlemeByLokasyonId(int lokasyonId, string userId);
        //Task<List<TesisGenelIzlemeModel>> GetTesisGenelIzlemeByLokasyonId(int lokasyonId);
        Task<TesisGenelKartModel> GetTesisGenelKartByLokasyonId(int lokasyonId);
        Task<TesisYemRasyonAyarModel?> GetTesisYemRasyonAyarlariByLokasyonId(int lokasyonId, CancellationToken cancellationToken);
        Task<List<KumesRaporModel>> GetKumeslerOzetRaporByLokasyonId(int lokasyonId, string userId);
        Task<List<KumesRaporModel>> GetKumeslerOzetRaporByLokasyonIdHub(int lokasyonId);
        Task<List<KumesRaporModel>> GetKumesRaporByKumesId(int kumesId, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval);
        Task<List<KumesRaporModel>> GetKumesYumurtaRaporByKumesId(int kumesId, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval);
        Task<List<KumesKarsilastirmaRaporModel>> GetKumesKarsilastirmaRaporu(List<int> kumesIds, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval);
        Task<List<KumesKarsilastirmaRaporModel>> GetKumesKarsilastirmaYumurtaRaporu(List<int> kumesIds, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval);
        Task<TesisGenelSnapshotModel> GetTesisGenelSnapshotByLokasyonId(int lokasyonId);
        Task<TesisGenelIzlemeYetkiModel> GetTesisGenelIzlemeYetkiByLokasyonId(int lokasyonId, string userId);
        Task<KumesScadaModel> GetKumesBilgileriByKumesId(int kumesId);
        Task<List<AlarmViewModel>> GetAlarmLogForKumes(int kumesId);
        Task<List<AlarmViewModel>> GetAktifAlarmLogForKumes(int kumesId);
        Task<List<AlarmViewModel>> GetPasifAlarmLogForKumes(int kumesId);
        Task<PlcHaberlesmeBilgiModel> GetKumesHaberlesmeBilgileriByKumesId(int kumesId);
        Task KumesHaberlesmeBilgileriDegistir(int kumesId, string plcIpAdres, bool plcAutoConnect, bool plcKumesAktif);
        //Task<bool> GetKumesAktifByKumesId(int kumesId);
        Task<KumesDonemModel?> GetKumesDonemBilgisiByKumesId(int kumesId);
        Task<int?> GetOlenHayvanSayisiByDate(int kumesId, DateTime tarih);
        Task AddKumesHayvanLog(int kumesId, int oluHayvanAdet, int saglikliHayvanAdet, DateTime tarih, string userId);
        Task KumesDonemBitir(int kumesId, string kumesDonemName, string userId);
        Task KumesDonemBaslat(int kumesId, DateTime girisTarihi, string cins, int hayvanAdet, string donemAdi, int girisHaftaYas, string userId);
        Task<List<HayvanCins>> GetAllHayvanCins();
        Task<List<FarmDataDto>> GetFarmDataDto();
        Task<List<FarmDataDto>> GetDamizlikFarmDataDto();
        Task<List<AnimalCountRangeItem>> GetTicariYumurtaKumesleriHayvanSayisi(int[] nodeIds, DateTime startTime, DateTime endTime, CancellationToken cancellationToken);
        Task<bool> SaveTesisYemRasyonAyarlariByLokasyonId(TesisYemRasyonAyarModel tesisYemRasyonAyarModel, CancellationToken cancellationToken);
        Task<List<KumesViewModel>> GetKarsilastirilabilirKumeslerByKumesId(int kumesId);
        Task<List<PlcBaglantiKesintiGunlukRaporModel>> GetPlcBaglantiKesintiRaporuByLokasyonId(int lokasyonId, DateTime baslangicZamani, DateTime bitisZamani);
        Task<List<PlcBaglantiKesintiLog>> GetPlcBaglantiKesintiDetayByKumesId(int kumesId, DateTime baslangicZamani, DateTime bitisZamani);

    }
}