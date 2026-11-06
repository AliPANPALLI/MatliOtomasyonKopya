using KumesScada.Domain.Interfaces;
using KumesScada.Domain.Models;
using KumesScada.Infrastructure.Repositories.Core;

namespace KumesScada.Infrastructure.Repositories
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