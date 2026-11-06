using PanelIdentification.Domain.Models;
using SharedKernel.Core;

namespace PanelIdentification.Domain.Interfaces
{
    public interface IBolumRepository : IRepository<Bolum>
    {
        Task<int> FindLokasyonIdByBolumId(int bolumId);
    }
}
