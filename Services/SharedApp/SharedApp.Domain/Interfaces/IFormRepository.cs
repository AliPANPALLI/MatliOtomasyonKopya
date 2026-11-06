using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Models;
using SharedKernel.Core;

namespace SharedApp.Domain.Interfaces
{
    public interface IFormRepository : IRepository<Form>
    {
        Task<List<vwForm>> GetAllvwFormsForYetkiByModulId(int modulId);
        Task<List<vwForm>> GetAllvwFormsForYetkiByModulIds(List<int> modulIds);
    }
}
