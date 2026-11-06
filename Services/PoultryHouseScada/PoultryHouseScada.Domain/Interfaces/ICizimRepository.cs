using PoultryHouseScada.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace PoultryHouseScada.Domain.Interfaces
{
    public interface ICizimRepository : IRepository<Cizim>
    {
        Task<List<int>> GetSekilIdsByKumesId(int kumesId);
        Task<List<CizimSekilEsleme>> GetCizimSekilEslemesByCizimId(int cizimId);
        Task<List<Sekil>> GetCizimEtiketler(List<int> sekilIds);
        Task<List<SekilEtiketKural>> GetEtiketKural();
        Task<List<SekilEtiketKural>> GetEtiketKural(List<int> sekilIds);
        Task<List<Etiket>> GetEtiketler();
        Task<int?> GetSekilIdByJsonId(string shapeJsonId);
        Task<SekilEtiketKural> SaveEtiketKural(SekilEtiketKural kural);
        Task DeleteEtiketKural(int id);
        //Task<List<EtiketLog_Temp>> GetEtiketLogVeriler();
        //Task<List<EtiketLog_Temp>> GetEtiketLogVerilerByKumesId(int kumesId);
        Task<List<EtiketLog_TempYeni>> GetEtiketLogVeriler();
        Task<List<EtiketLog_TempYeni>> GetEtiketLogVerilerByKumesId(int kumesId);
        Task<List<EtiketLogKonveyor>> GetEtiketLogKonveyorVeriler();
        Task EditSekil(SekilJson sekil);
    }
}
