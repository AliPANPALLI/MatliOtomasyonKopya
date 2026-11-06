using PanelIdentification.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace PanelIdentification.Domain.Interfaces
{
    public interface IBolumRepository : IRepository<Bolum>
    {
        Task<int> FindLokasyonIdByBolumId(int bolumId);
    }
}
