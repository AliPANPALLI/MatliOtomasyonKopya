using PoultryHouseScada.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace PoultryHouseScada.Domain.Interfaces
{
    public interface IKumesLogTempRepository : IRepository<KumesLog_TempYeni>
    {
    }
}
