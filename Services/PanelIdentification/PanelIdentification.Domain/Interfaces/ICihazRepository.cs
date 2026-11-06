using PanelIdentification.Domain.Models;
using SharedKernel.Core;

namespace PanelIdentification.Domain.Interfaces
{
    public interface ICihazRepository : IRepository<Cihaz>
    {
        Task<int> FindLokasyonIdByCihazId(int cihazId);
    }
}
