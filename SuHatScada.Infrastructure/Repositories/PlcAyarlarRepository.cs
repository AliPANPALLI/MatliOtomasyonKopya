using SuHatScada.Domain.Interfaces;
using SuHatScada.Domain.Models;
using SuHatScada.Infrastructure.Repositories.Core;

namespace SuHatScada.Infrastructure.Repositories
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