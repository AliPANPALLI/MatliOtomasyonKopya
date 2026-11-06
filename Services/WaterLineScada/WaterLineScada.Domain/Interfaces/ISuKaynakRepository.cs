using WaterLineScada.Domain.DbViewModels;
using WaterLineScada.Domain.Models;
using WaterLineScada.Domain.Models.ReportModels;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace WaterLineScada.Domain.Interfaces
{
    public interface ISuKaynakRepository : IRepository<SuKaynak>
    {
        Task<List<Cizim>> GetAllCizimForYetki();
        Task<List<vwCizimLokasyonEsleme>> GetAllSuKaynakCizimForYetki();
        Task<List<AlarmLog>> GetAlarmLogForSuIzleme(int lokasyonId);
        Task<List<vwAlarmLog>> GetAktifAlarmLogForSuIzleme(int cizimId);
        Task<List<vwAlarmLog>> GetPasifAlarmLogForSuIzleme(int cizimId);
        Task<List<AlarmLog>> GetAktifAlarmLogForSuIzlemeByLokasyonId(int lokasyonId);
        Task<List<CizimLokasyonEsleme>> GetCizimLokasyonEslemeFromCizimId(int cizimId);
        Task<List<vwCizimLokasyonEsleme>> GetVwCizimLokasyonEslemeFromCizimId(int cizimId);
        Task<List<CizimLokasyonEsleme>> GetCizimLokasyonEslemeFromLokasyonId(int lokasyonId);
        Task<List<vwLokasyon>> GetLokasyonlarFromLokasyonIds(List<int> lokasyonIds);
        //Task<List<EtiketLog>> GetVerilerFromSuKaynakId(List<int> suKaynakIds);
        Task<List<EtiketLogYeni>> GetVerilerFromSuKaynakId(List<int> suKaynakIds);
        Task<List<TimePeriodData>> GetToplamSuDataFromView(string viewName, int sayacId, DateTime startDate, DateTime endDate);
        Task<List<TimePeriodData>> GetTankSuDataFromView(string periyot, int suKaynakId, string tankName, DateTime startDate, DateTime endDate);
    }
}