using SharedApp.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace SharedApp.Domain.Interfaces
{
    public interface IGenelParametrelerRepository : IRepository<GenelParametreler>
    {
        Task<GenelParametreler?> GetGenelParametrelerDegerByName(string genelParametreName);
    }
}
