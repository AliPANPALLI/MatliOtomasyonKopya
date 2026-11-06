using PoultryHouseScada.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace PoultryHouseScada.Domain.Interfaces
{
    public interface IRaporVeriRepository : IRepository<RaporVeri>
    {
        Task<List<RaporVeri>> GetByKategoriAdi(string kategoriAdi);
    }
}
