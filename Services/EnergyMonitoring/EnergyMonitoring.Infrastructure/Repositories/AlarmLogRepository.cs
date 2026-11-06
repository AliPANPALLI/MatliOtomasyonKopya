using EnergyMonitoring.Domain.DbViewModels;
using EnergyMonitoring.Domain.Interfaces;
using EnergyMonitoring.Domain.Models;
using EnergyMonitoring.Infrastructure.Repositories.Core;
using Microsoft.EntityFrameworkCore;

namespace EnergyMonitoring.Infrastructure.Repositories
{
    public class AlarmLogRepository : Repository<AlarmLog>, IAlarmLogRepository
    {
        private EnerjiIzlemeDbContext _context;

        public AlarmLogRepository(EnerjiIzlemeDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<vwAlarmLog>> GetAktifAlarmLogForEnerjiIzleme(int lokasyonId)
        {
            var alarmLogs = await _context.vwAlarmLog.Where(x => x.LokasyonId == lokasyonId && x.Aktif != false /*&& x.KayitZamani.Value.Date == DateTime.Now.Date*/ && x.Silindi != true /*&& x.AlarmScadaEkrandaGosterilsinMi != false*/).OrderByDescending(x => x.KayitZamani).ToListAsync();
            return alarmLogs;
        }

        public async Task<List<vwAlarmLog>> GetPasifAlarmLogForEnerjiIzleme(int lokasyonId)
        {
            var alarmLogs = await _context.vwAlarmLog.Where(x => x.LokasyonId == lokasyonId && x.Aktif == false && x.KayitZamani.Value.Date >= DateTime.Now.Date.AddDays(-10) && x.Silindi != true /*&& x.AlarmScadaEkrandaGosterilsinMi != false*/).OrderByDescending(x => x.KayitZamani).Take(500).ToListAsync();
            return alarmLogs;
        }
    }
}