using PanoKimliklendirme.Domain.Models;
using SharedKernel.Core;

namespace PanoKimliklendirme.Domain.Interfaces
{
    public interface IPanoRepository : IRepository<Pano>
    {
        Task<int> FindLokasyonIdByPanoId(int panoId);
        Task<List<Tuple<string, int, string>>> GetParentNodesByPanoIdForDosyaYolu(int panoId);
    }
}
