using EnerjiIzleme.Domain.DbViewModels;
using EnerjiIzleme.Domain.Interfaces;
using EnerjiIzleme.Domain.Models;
using EnerjiIzleme.Infrastructure.Repositories.Core;
using Microsoft.EntityFrameworkCore;

namespace EnerjiIzleme.Infrastructure.Repositories
{
    public class MakineRepository : Repository<Makine>, IMakineRepository
    {
        private EnerjiIzlemeDbContext _context;

        public MakineRepository(EnerjiIzlemeDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task AddMakine(Makine makine)
        {
            await _context.Makine.AddAsync(makine);
            await _context.SaveChangesAsync();
        }

        public async Task<vwMakine> GetvwMakineById(int id)
        {
            return await _context.vwMakine.Where(x => x.Id == id).FirstAsync();
        }

        public async Task SetSilindi(int id)
        {
            var makine = await _context.Makine.Where(x => x.Id == id).FirstOrDefaultAsync();
            if (makine != null)
            {
                makine.Silindi = true;
                var cihazlar = await _context.Cihaz.Where(x => x.MakineId == makine.Id).ToListAsync();
                foreach (var cihaz in cihazlar)
                {
                    cihaz.Aktif = false;
                    cihaz.Silindi = true;
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}