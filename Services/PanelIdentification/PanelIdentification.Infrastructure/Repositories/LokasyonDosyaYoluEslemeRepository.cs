using PanelIdentification.Domain.Interfaces;
using PanelIdentification.Domain.Models;
using PanelIdentification.Infrastructure.Repositories.Core;

namespace PanelIdentification.Infrastructure.Repositories
{
    public class LokasyonDosyaYoluEslemeRepository : Repository<LokasyonDosyaYoluEsleme>, ILokasyonDosyaYoluEslemeRepository
    {
        private readonly PanoKimliklendirmeDbContext _context;
        public LokasyonDosyaYoluEslemeRepository(PanoKimliklendirmeDbContext context) : base(context)
        {
            _context = context;
        }       
    }
}
