using PanoKimliklendirme.Domain.Models;
using SharedKernel.Core;

namespace PanoKimliklendirme.Domain.Interfaces
{
    public interface ICihazRepository : IRepository<Cihaz>
    {
        Task<int> FindLokasyonIdByCihazId(int cihazId);
    }
}
