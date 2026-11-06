using PanoKimliklendirme.Domain.Models;
using SharedKernel.Core;

namespace PanoKimliklendirme.Domain.Interfaces
{
    public interface IMakineRepository : IRepository<Makine>
    {
        Task<int> FindLokasyonIdByMakineId(int makineId);
    }
}
