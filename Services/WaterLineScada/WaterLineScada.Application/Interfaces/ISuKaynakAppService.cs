using WaterLineScada.Application.ViewModels;
using WaterLineScada.Domain.DbViewModels;
using WaterLineScada.Domain.Models;
using WaterLineScada.Domain.Models.ReportModels;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace WaterLineScada.Application.Interfaces
{
    public interface ISuKaynakAppService : IAppService<SuKaynakViewModel>
    {
        Task<List<SuKaynak>> GetAllSuKaynakForYetki();
        Task<List<vwCizimLokasyonEsleme>> GetAllSuKaynakCizimForYetki();
        Task<List<Cizim>> GetAllCizimForYetki();
        Task<List<int>> GetLokasyonIdsBySuKaynakIds(List<int> suKaynakIds);
        Task<List<SuKaynakRaporModel>> GetSuHatRaporByCizimId(int cizimId, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip);
        Task<List<SuKaynakRaporModel>> GetSuHatRaporByLokasyonIds(List<int> lokasyonIds, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip);
        Task<List<SuKaynakRaporModel>> GetSuHatRaporByCizimId2(int cizimId, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip, CancellationToken cancellationToken = default); Task<List<SuKaynakRaporModel>> GetSuHatRaporByCizimIdYeni(int cizimId, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip);
        Task<List<Tuple<string, int>>> GetLokasyonNamesForSuHatRaporByCizimId(int cizimId);
        Task<List<SuKaynakRaporModel>> GetSuHatRaporByLokasyonId(int lokasyonId, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip);
        Task<List<AlarmViewModel>> GetAlarmLogForSuIzleme(int lokasyonId);
        Task<List<AlarmViewModel>> GetAktifAlarmLogForSuIzleme(int cizimId);
        Task<List<AlarmViewModel>> GetPasifAlarmLogForSuIzleme(int cizimId);
        Task<List<AlarmViewModel>> GetAktifAlarmLogForSuIzlemeByLokasyonId(int lokasyonId);
        Task<List<SuHatPlcAyarlarModel>> GetSuHatPlcAyarlar();
        Task<SuHatPlcAyarlarModel> GetSuHatPlcAyarlarBySuLokasyonId(int suLokasyonId);
        Task<List<SuHatRaporLokasyon>> GetRaporLokasyonlarByCizimId(int cizimId);
        Task SuKaynakHaberlesmeBilgileriDegistir(int suLokasyonId, string plcIpAdres, bool plcAutoConnect, bool aktif);
    }
}