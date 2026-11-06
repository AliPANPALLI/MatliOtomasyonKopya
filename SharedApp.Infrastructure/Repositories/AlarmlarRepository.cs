using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;

namespace SharedApp.Infrastructure.Repositories
{
    public class AlarmlarRepository : Repository<Alarmlar>, IAlarmlarRepository
    {
        private readonly SharedDbContext _context;
        public AlarmlarRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<vwAlarmlar>> GetAllvwAlarmlarByAlarmGrupIdDx(int alarmGrupId)
        {
            var alarmGrupAlarmEslemeler = await _context.AlarmGrupAlarmEsleme.AsNoTracking().Where(x => x.AlarmGrupId == alarmGrupId && x.Silindi != true).ToListAsync();
            var alarmlar = await _context.vwAlarmlar.AsNoTracking().Where(x => alarmGrupAlarmEslemeler.Select(y => y.AlarmId).ToList().Contains(x.Id) && x.Silindi != true).ToListAsync();
            return alarmlar;
        }

        public async Task<List<vwAlarmlar>> GetAllvwAlarmlarDx()
        {
            return await _context.vwAlarmlar.AsNoTracking().Where(x => x.Silindi != true).ToListAsync();
        }
    }
}