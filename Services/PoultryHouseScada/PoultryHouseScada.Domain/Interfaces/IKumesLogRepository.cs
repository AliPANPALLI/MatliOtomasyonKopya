using PoultryHouseScada.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace PoultryHouseScada.Domain.Interfaces
{
    public interface IKumesLogRepository : IRepository<KumesLogYeni>
    {
        //Task<List<KumesLog?>?> GetLastKumesLogs();
        //Task<List<KumesLog?>?> GetLastTenKumesLogs();
        //Task<List<KumesLog?>?> GetLastKumesLogsByKumesIds(List<int> kumesIds);
        Task<List<KumesLogYeni?>?> GetLastKumesLogs();
        Task<List<KumesLogYeni?>?> GetLastTenKumesLogs();
        Task<List<KumesLogYeni?>?> GetLastKumesLogsByKumesIds(List<int> kumesIds);
    }
}