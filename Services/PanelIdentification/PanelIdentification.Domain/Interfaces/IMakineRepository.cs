using PanelIdentification.Domain.Models;
using SharedKernel.Core;

namespace PanelIdentification.Domain.Interfaces
{
    public interface IMakineRepository : IRepository<Makine>
    {
        Task<int> FindLokasyonIdByMakineId(int makineId);
    }
}
