using PoultryHouseScada.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace PoultryHouseScada.Domain.Interfaces
{
    public interface IKumesRaporRehberRepository : IRepository<KumesRaporRehber>
    {
        Task<List<KumesRaporRehber>> GetByLokasyonId(int lokasyonId);
        Task<List<KumesRaporRehber>> GetKumesRaporRehberByLokasyonIdAndGuideTipi(int lokasyonId, string guideTipi, int hayvanCinsId);
    }
}