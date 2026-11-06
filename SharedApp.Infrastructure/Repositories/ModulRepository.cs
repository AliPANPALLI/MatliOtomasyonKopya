using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;

namespace SharedApp.Infrastructure.Repositories
{
    public class ModulRepository : Repository<Modul>, IModulRepository
    {
        private readonly SharedDbContext _context;
        public ModulRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }

    }
}
