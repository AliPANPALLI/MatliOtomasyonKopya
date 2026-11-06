using PoultryHouseScada.Domain.Interfaces;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure.Repositories.Core;

namespace PoultryHouseScada.Infrastructure.Repositories
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
