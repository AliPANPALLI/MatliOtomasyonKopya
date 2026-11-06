using EnergyMonitoring.Domain.Interfaces;
using EnergyMonitoring.Domain.Models;
using EnergyMonitoring.Infrastructure.Repositories.Core;
using Microsoft.EntityFrameworkCore;

namespace EnergyMonitoring.Infrastructure.Repositories
{
    public class CihazModelRepository : Repository<CihazModel>, ICihazModelRepository
    {
        private EnerjiIzlemeDbContext _context;

        public CihazModelRepository(EnerjiIzlemeDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task AddCihazModel(CihazModel cihazModel)
        {
            await _context.CihazModel.AddAsync(cihazModel);
            await _context.SaveChangesAsync();
        }

        public async Task SetSilindi(int id)
        {
            var cihazModel = await _context.CihazModel.Where(x => x.Id == id).FirstOrDefaultAsync();
            if (cihazModel != null)
            {
                cihazModel.Silindi = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}