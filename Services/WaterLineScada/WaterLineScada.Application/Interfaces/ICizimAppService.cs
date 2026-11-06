using SharedKernel.Core;
using WaterLineScada.Application.ViewModels;
using WaterLineScada.Domain.Models;

namespace WaterLineScada.Application.Interfaces
{
    public interface ICizimAppService : IAppService<CizimViewModel>
    {
        Task<List<SekilJson>> GetCizimJsonByNameWithValue(string cizimName);
        Task<List<SekilJson>> GetCizimJsonById(int cizimId);
        Task<List<SekilJson>> GetCizimJsonByLokasyonId(int lokasyonId);
        Task<List<SekilJson>> GetCizimJsonByLokasyonIdAndUserId(int lokasyonId, string userId);
        Task EditSekil(SekilJson sekil);
        Task<ScadaData> GetScadaDataByLokasyonIdAndUserId(int lokasyonId, string userId);
        Task<List<KullaniciSekilYetki>> GetKullaniciSekilYetki(int lokasyonId, string userId);
        Task<List<SekilEtiketKural>> GetSekilEtiketKural(int lokasyonId, string userId);
        Task<List<SekilEtiketKural>> GetSekilEtiketKuralByCizimId(int cizimId, string userId);
        Task<List<EtiketLog_TempYeni>> GetEtiketLog(int lokasyonId);
        Task<List<Sekil>> GetSekil(int lokasyonId);
        Task<List<Sekil>> GetSekilByCizimId(int cizimId);
        Task<ScadaData> GetScadaData(int lokasyonId);
        Task<ScadaData> GetScadaDataByCizimId(int cizimId);
        Task UpdateDMModTuru(int cizimId, string modTuru, string userId);
        Task UpdateTankModTuru(int cizimId, string modTuru, string userId);
        Task UpdateSekilModTuru2(string sekilId, string targetSekilId, string modTuru, string userId);
        Task UpdateSekilPozisyon(string sekilId, int pozisyonX, int pozisyonY, string userId);
        Task UpdateSekilVisible(string sekilId, bool gorunsunMu, string userId);
        Task<string> GetCizimUrlById(int cizimId);
        Task<Cizim?> GetCizimById(int cizimId);
    }
}