using SharedApp.Domain.Models;
using SharedKernel.Core;

namespace SharedApp.Domain.Interfaces
{
    public interface IGenelParametrelerRepository : IRepository<GenelParametreler>
    {
        Task<GenelParametreler?> GetGenelParametrelerDegerByName(string genelParametreName);
    }
}
