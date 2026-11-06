using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;

namespace SharedApp.Infrastructure.Repositories
{
    public class FormLinkEslemeRepository : Repository<FormLinkEsleme>, IFormLinkEslemeRepository
    {
        private readonly SharedDbContext _context;

        public FormLinkEslemeRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<FormLinkEsleme>> GetAllByFormId(int formId)
        {
            return await _context.Set<FormLinkEsleme>()
                .AsNoTracking()
                .Where(x => x.FormId == formId && x.Silindi != true)
                .OrderBy(x => x.Sira)
                .ToListAsync();
        }

        public async Task<List<FormLinkEsleme>> GetAllByModulId(int modulId)
        {
            var query =
                from fl in _context.Set<FormLinkEsleme>().AsNoTracking()
                join f in _context.Form.AsNoTracking() on fl.FormId equals f.Id
                where f.ModulId == modulId && f.Silindi != true && fl.Silindi != true
                orderby fl.Sira
                select fl;

            return await query.ToListAsync();
        }
    }
}
