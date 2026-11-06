using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;

namespace SharedApp.Infrastructure.Repositories
{
    public class AlarmGrupAlarmEslemeRepository : Repository<AlarmGrupAlarmEsleme>, IAlarmGrupAlarmEslemeRepository
    {
        private readonly SharedDbContext _context;
        public AlarmGrupAlarmEslemeRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }
    }
}