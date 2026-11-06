using SharedKernel.Core;
using SuHatScada.Domain.Models;

namespace SuHatScada.Domain.Interfaces
{
    public interface IEtiketLogRepository : IRepository<EtiketLogYeni>
    {
        Task<List<EtiketLogYeni>> GetEtiketLogGunSonuVeriler(IReadOnlyCollection<int> etiketIds);
    }
}
