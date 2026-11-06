using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Models;
using SharedKernel.Core;

namespace SharedApp.Domain.Interfaces
{
    public interface IFormLokasyonEslemeRepository : IRepository<FormLokasyonEsleme>
    {
        IQueryable<vwFormLokasyonEsleme> GetAllFormLokasyonEslemeDx();
    }
}
