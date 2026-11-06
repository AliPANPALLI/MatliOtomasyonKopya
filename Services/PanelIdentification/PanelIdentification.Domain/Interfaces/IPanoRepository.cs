using PanelIdentification.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace PanelIdentification.Domain.Interfaces
{
    public interface IPanoRepository : IRepository<Pano>
    {
        Task<int> FindLokasyonIdByPanoId(int panoId);
        Task<List<Tuple<string, int, string>>> GetParentNodesByPanoIdForDosyaYolu(int panoId);
    }
}
