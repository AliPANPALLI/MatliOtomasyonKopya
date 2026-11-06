using WaterLineScada.Domain.Interfaces;
using WaterLineScada.Domain.Models;
using WaterLineScada.Infrastructure.Repositories.Core;

namespace WaterLineScada.Infrastructure.Repositories
{
    public class PlcAyarlarRepository : Repository<PlcAyarlar>, IPlcAyarlarRepository
    {
        private readonly SuHatScadaDbContext _context;
        public PlcAyarlarRepository(SuHatScadaDbContext context) : base(context)
        {
            _context = context;
        }
    }
}