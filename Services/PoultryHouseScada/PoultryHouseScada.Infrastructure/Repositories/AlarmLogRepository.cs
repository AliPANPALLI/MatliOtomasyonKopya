using PoultryHouseScada.Domain.Interfaces;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure.Repositories.Core;

namespace PoultryHouseScada.Infrastructure.Repositories
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
