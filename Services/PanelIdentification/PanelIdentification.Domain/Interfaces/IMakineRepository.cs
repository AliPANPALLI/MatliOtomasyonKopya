using PanelIdentification.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace PanelIdentification.Domain.Interfaces
{
    public interface IMakineRepository : IRepository<Makine>
    {
        Task<int> FindLokasyonIdByMakineId(int makineId);
    }
}
