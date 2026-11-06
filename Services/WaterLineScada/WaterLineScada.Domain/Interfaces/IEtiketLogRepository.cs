using SharedKernel.Core;
using WaterLineScada.Domain.Models;

namespace WaterLineScada.Domain.Interfaces
{
    public interface IEtiketLogRepository : IRepository<EtiketLogYeni>
    {
        Task<List<EtiketLogYeni>> GetEtiketLogGunSonuVeriler(IReadOnlyCollection<int> etiketIds);
    }
}
