using PoultryHouseScada.Domain.Interfaces;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure.Repositories.Core;

namespace PoultryHouseScada.Infrastructure.Repositories
{
    public class KumesHayvanLogRepository : Repository<KumesHayvanLog>, IKumesHayvanLogRepository
    {
        private readonly KumesScadaDbContext _context;
        public KumesHayvanLogRepository(KumesScadaDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
