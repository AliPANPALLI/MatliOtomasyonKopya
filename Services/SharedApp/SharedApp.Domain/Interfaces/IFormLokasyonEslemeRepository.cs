using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace SharedApp.Domain.Interfaces
{
    public interface IFormLokasyonEslemeRepository : IRepository<FormLokasyonEsleme>
    {
        IQueryable<vwFormLokasyonEsleme> GetAllFormLokasyonEslemeDx();
    }
}
