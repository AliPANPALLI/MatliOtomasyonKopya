using PoultryHouseScada.Domain.Interfaces;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure.Repositories.Core;

namespace PoultryHouseScada.Infrastructure.Repositories
{
    public class KumesHaftalikCatiKontrolRepository : Repository<KumesHaftalikCatiKontrol>, IKumesHaftalikCatiKontrolRepository
    {
        private readonly KumesScadaDbContext _context;
        public KumesHaftalikCatiKontrolRepository(KumesScadaDbContext context) : base(context)
        {
            _context = context;
        }
    }
}