using KumesScada.Domain.Interfaces;
using KumesScada.Domain.Models;
using KumesScada.Infrastructure.Repositories.Core;

namespace KumesScada.Infrastructure.Repositories
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
