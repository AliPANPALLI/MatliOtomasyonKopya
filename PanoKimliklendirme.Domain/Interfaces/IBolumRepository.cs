using PanoKimliklendirme.Domain.Models;
using SharedKernel.Core;

namespace PanoKimliklendirme.Domain.Interfaces
{
    public interface IBolumRepository : IRepository<Bolum>
    {
        Task<int> FindLokasyonIdByBolumId(int bolumId);
    }
}
