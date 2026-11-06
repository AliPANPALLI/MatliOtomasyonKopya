using KumesScada.Domain.Models;
using SharedKernel.Core;

namespace KumesScada.Domain.Interfaces
{
    public interface IRaporVeriRepository : IRepository<RaporVeri>
    {
        Task<List<RaporVeri>> GetByKategoriAdi(string kategoriAdi);
    }
}
