using PoultryHouseScada.Domain.Interfaces;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure.Repositories.Core;

namespace PoultryHouseScada.Infrastructure.Repositories
{
    public class KumesLogTempRepository : Repository<KumesLog_TempYeni>, IKumesLogTempRepository
    {
        private readonly KumesScadaDbContext _context;
        public KumesLogTempRepository(KumesScadaDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
