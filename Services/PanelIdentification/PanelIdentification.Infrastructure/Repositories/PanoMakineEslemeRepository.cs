using PanelIdentification.Domain.Interfaces;
using PanelIdentification.Domain.Models;
using PanelIdentification.Infrastructure.Repositories.Core;

namespace PanelIdentification.Infrastructure.Repositories
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
