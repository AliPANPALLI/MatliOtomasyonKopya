using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;

namespace SharedApp.Infrastructure.Repositories
{
    public class FormLokasyonEslemeRepository : Repository<FormLokasyonEsleme>, IFormLokasyonEslemeRepository
    {
        private readonly SharedDbContext _context;
        public FormLokasyonEslemeRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }
        public IQueryable<vwFormLokasyonEsleme> GetAllFormLokasyonEslemeDx()
        {
            return _context.vwFormLokasyonEsleme.Where(x => !(x.FormSilindi ?? false) && !(x.ModulSilindi ?? false) && !(x.SirketSilindi ?? false) && !(x.LokasyonSilindi ?? false));
        }
    }
}