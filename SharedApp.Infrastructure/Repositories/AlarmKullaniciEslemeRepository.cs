using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;

namespace SharedApp.Infrastructure.Repositories
{
    public class AlarmKullaniciEslemeRepository : Repository<AlarmKullaniciEsleme>, IAlarmKullaniciEslemeRepository
    {
        private readonly SharedDbContext _context;
        public AlarmKullaniciEslemeRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<vwAlarmKullaniciEsleme>> GetAllvwAlarmKullaniciEsleme()
        {
            return await _context.vwAlarmKullaniciEsleme.AsNoTracking().Where(x => x.Silindi != true).ToListAsync();
        }
    }
}