using PanelIdentification.Domain.Models;
using SharedKernel.Core;

namespace PanelIdentification.Domain.Interfaces
{
    public interface IPanoRepository : IRepository<Pano>
    {
        Task<int> FindLokasyonIdByPanoId(int panoId);
        Task<List<Tuple<string, int, string>>> GetParentNodesByPanoIdForDosyaYolu(int panoId);
    }
}
