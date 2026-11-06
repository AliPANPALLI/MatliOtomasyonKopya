using WaterLineScada.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace WaterLineScada.Domain.Interfaces
{
    public interface IEtiketLogRepository : IRepository<EtiketLogYeni>
    {
        Task<List<EtiketLogYeni>> GetEtiketLogGunSonuVeriler(IReadOnlyCollection<int> etiketIds);
    }
}
