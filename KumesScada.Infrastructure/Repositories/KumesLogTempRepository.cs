using KumesScada.Domain.Interfaces;
using KumesScada.Domain.Models;
using KumesScada.Infrastructure.Repositories.Core;

namespace KumesScada.Infrastructure.Repositories
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
