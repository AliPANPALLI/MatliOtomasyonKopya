using KumesScada.Domain.Interfaces;
using KumesScada.Domain.Models;
using KumesScada.Infrastructure.Repositories.Core;

namespace KumesScada.Infrastructure.Repositories
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
