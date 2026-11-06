using PoultryHouseScada.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace PoultryHouseScada.Domain.Interfaces
{
    public interface INekropsiRaporuRepository : IRepository<NekropsiRaporu>
    {
        Task<List<NekropsiRaporu>> GetAllWithDetailsByKumesIdAsync(int kumesId);
        Task<NekropsiRaporu?> GetWithDetailsByIdAsync(int id);
        Task<NekropsiFotograf?> GetFotografByIdAsync(int fotografId);
        Task GuncelleAsync(NekropsiRaporu guncellenecekRapor, List<NekropsiBulgu> yeniBulgular);
        Task<NekropsiFotograf?> FotografSilAsync(int fotografId);
    }
}
