using PanelIdentification.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace PanelIdentification.Domain.Interfaces
{
    public interface ICihazRepository : IRepository<Cihaz>
    {
        Task<int> FindLokasyonIdByCihazId(int cihazId);
    }
}
