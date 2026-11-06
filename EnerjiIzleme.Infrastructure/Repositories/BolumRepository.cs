using EnerjiIzleme.Domain.DbViewModels;
using EnerjiIzleme.Domain.Interfaces;
using EnerjiIzleme.Domain.Models;
using EnerjiIzleme.Infrastructure.Repositories.Core;
using Microsoft.EntityFrameworkCore;

namespace EnerjiIzleme.Infrastructure.Repositories
{
    public class BolumRepository : Repository<Bolum>, IBolumRepository
    {
        private EnerjiIzlemeDbContext _context;

        public BolumRepository(EnerjiIzlemeDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task AddBolum(Bolum bolum)
        {
            await _context.Bolum.AddAsync(bolum);
            await _context.SaveChangesAsync();
        }

        public async Task<vwBolum> GetvwBolumById(int id)
        {
            return await _context.vwBolum.Where(x => x.Id == id).FirstAsync();
        }

        public async Task SetSilindi(int id)
        {
            var bolum = await _context.Bolum.Where(x => x.Id == id && x.Silindi != true).FirstOrDefaultAsync();
            if (bolum != null)
            {
                bolum.Silindi = true;

                var makineler = await _context.Makine.Where(x => x.BolumId == id).ToListAsync();
                foreach (var makine in makineler)
                {
                    makine.Silindi = true;
                }
                var cihazlar = await _context.Cihaz.Where(x => makineler.Select(y => y.Id).ToList().Contains(x.MakineId)).ToListAsync();
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