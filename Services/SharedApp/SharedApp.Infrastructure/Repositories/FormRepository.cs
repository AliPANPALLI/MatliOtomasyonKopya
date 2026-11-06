using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;

namespace SharedApp.Infrastructure.Repositories
{
    public class FormRepository : Repository<Form>, IFormRepository
    {
        private readonly SharedDbContext _context;
        public FormRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<vwForm>> GetAllvwFormsForYetkiByModulId(int modulId)
        {
            return await _context.vwForm.Where(x => x.ModulId == modulId /*&& (x.TopbarNesnesi ?? false)*/ && x.FormSilindi != true).ToListAsync();
        }

        public async Task<List<vwForm>> GetAllvwFormsForYetkiByModulIds(List<int> modulIds)
        {
            return await _context.vwForm.Where(x => modulIds.Contains(x.ModulId)/*&& (x.TopbarNesnesi ?? false)*/ && x.FormSilindi != true).ToListAsync();
        }
    }
}
