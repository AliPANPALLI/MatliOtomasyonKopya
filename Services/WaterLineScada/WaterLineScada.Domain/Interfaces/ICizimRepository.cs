using WaterLineScada.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace WaterLineScada.Domain.Interfaces
{
    public interface ICizimRepository : IRepository<Cizim>
    {
        Task<List<CizimSekilEsleme>> GetCizimSekilEslemesByCizimId(int cizimId);
        Task<List<Sekil>> GetCizimEtiketler(List<int> sekilIds);
        Task<int> GetCizimIdByLokasyonId(int lokasyonId);
        Task<List<SekilEtiketKural>> GetEtiketKural(List<int> sekilIds);
        //Task<List<EtiketLog_Temp>> GetEtiketLogVerilerByCizimId(int cizimId);
        //Task<List<EtiketLog_Temp>> GetEtiketLogVerilerByEtiketIds(List<int> etiketIds);
        //Task<List<EtiketLog_Temp>> GetEtiketLogVeriler();
        //EtiketLog? GetEtiketLogGunSonuVeri(int etiketId);
        //Task<List<EtiketLog>> GetEtiketLogGunSonuVeriler(List<int> etiketIds);
        Task<List<EtiketLog_TempYeni>> GetEtiketLogVerilerByCizimId(int cizimId);
        Task<List<EtiketLog_TempYeni>> GetEtiketLogVerilerByEtiketIds(List<int> etiketIds);
        Task<List<EtiketLog_TempYeni>> GetEtiketLogVeriler();
        EtiketLogYeni? GetEtiketLogGunSonuVeri(int etiketId);
        Task<List<EtiketLogYeni>> GetEtiketLogGunSonuVeriler(List<int> etiketIds);
        Task EditSekil(SekilJson sekil);
        Task<List<KullaniciSekilYetki>> GetKullaniciSekilYetki2(List<int> sekilIds, string userId);
        Task<List<KullaniciSekilYetki>> GetKullaniciSekilYetki(int lokasyonId, string userId);
        Task UpdateDMModTuru(int cizimId, string modTuru, string userId);
        Task UpdateTankModTuru(int cizimId, string modTuru, string userId);
        Task UpdateSekilModTuru2(string sekilId, string targetSekilId, string modTuru, string userId);
        Task UpdateSekilPozisyon(string sekilId, int pozisyonX, int pozisyonY, string userId);
        Task UpdateSekilVisible(string sekilId, bool gorunsunMu, string userId);
    }
}
