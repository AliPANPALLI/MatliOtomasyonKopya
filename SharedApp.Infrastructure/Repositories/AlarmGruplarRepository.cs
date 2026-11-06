using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;

namespace SharedApp.Infrastructure.Repositories
{
    public class AlarmGruplarRepository : Repository<AlarmGruplar>, IAlarmGruplarRepository
    {
        private readonly SharedDbContext _context;
        public AlarmGruplarRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }
    }
}