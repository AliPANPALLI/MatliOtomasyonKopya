using PoultryHouseScada.Domain.Interfaces;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure.Repositories.Core;

namespace PoultryHouseScada.Infrastructure.Repositories
{
    public class KumesHayvanRepository : Repository<KumesHayvan>, IKumesHayvanRepository
    {
        private readonly KumesScadaDbContext _context;
        public KumesHayvanRepository(KumesScadaDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
