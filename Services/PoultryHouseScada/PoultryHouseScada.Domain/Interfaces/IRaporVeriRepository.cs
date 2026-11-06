using PoultryHouseScada.Domain.Models;
using SharedKernel.Core;

namespace PoultryHouseScada.Domain.Interfaces
{
    public interface IRaporVeriRepository : IRepository<RaporVeri>
    {
        Task<List<RaporVeri>> GetByKategoriAdi(string kategoriAdi);
    }
}
