using PoultryHouseScada.Domain.Interfaces;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure.Repositories.Core;

namespace PoultryHouseScada.Infrastructure.Repositories
{
    public class EtiketLogTempRepository : Repository<EtiketLog_TempYeni>, IEtiketLogTempRepository
    {
        private readonly KumesScadaDbContext _context;
        public EtiketLogTempRepository(KumesScadaDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
