using KumesScada.Domain.Interfaces;
using KumesScada.Domain.Models;
using KumesScada.Infrastructure.Repositories.Core;

namespace KumesScada.Infrastructure.Repositories
{
    public class AlarmLogRepository : Repository<AlarmLog>, IAlarmLogRepository
    {
        private readonly KumesScadaDbContext _context;
        public AlarmLogRepository(KumesScadaDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
