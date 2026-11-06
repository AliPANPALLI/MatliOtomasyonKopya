using PanoKimliklendirme.Domain.Interfaces;
using PanoKimliklendirme.Domain.Models;
using PanoKimliklendirme.Infrastructure.Repositories.Core;

namespace PanoKimliklendirme.Infrastructure.Repositories
{
    public class PanoMakineEslemeRepository : Repository<PanoMakineEsleme>, IPanoMakineEslemeRepository
    {
        private readonly PanoKimliklendirmeDbContext _context;
        public PanoMakineEslemeRepository(PanoKimliklendirmeDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
