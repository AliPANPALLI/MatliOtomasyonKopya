using PoultryHouseScada.Domain.Models;
using SharedKernel.Core;

namespace PoultryHouseScada.Domain.Interfaces
{
    public interface IKumesRaporRehberRepository : IRepository<KumesRaporRehber>
    {
        Task<List<KumesRaporRehber>> GetByLokasyonId(int lokasyonId);
        Task<List<KumesRaporRehber>> GetKumesRaporRehberByLokasyonIdAndGuideTipi(int lokasyonId, string guideTipi, int hayvanCinsId);
    }
}