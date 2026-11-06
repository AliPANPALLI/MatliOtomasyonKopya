using KumesScada.Domain.Models;
using SharedKernel.Core;

namespace KumesScada.Domain.Interfaces
{
    public interface IKumesRaporRehberRepository : IRepository<KumesRaporRehber>
    {
        Task<List<KumesRaporRehber>> GetByLokasyonId(int lokasyonId);
        Task<List<KumesRaporRehber>> GetKumesRaporRehberByLokasyonIdAndGuideTipi(int lokasyonId, string guideTipi, int hayvanCinsId);
    }
}