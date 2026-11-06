using KumesScada.Domain.Interfaces;
using KumesScada.Domain.Models;
using KumesScada.Infrastructure.Repositories.Core;

namespace KumesScada.Infrastructure.Repositories
{
    public class DonemRepository : Repository<Donem>, IDonemRepository
    {
        private readonly KumesScadaDbContext _context;
        public DonemRepository(KumesScadaDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
