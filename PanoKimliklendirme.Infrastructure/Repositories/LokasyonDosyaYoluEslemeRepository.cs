using PanoKimliklendirme.Domain.Interfaces;
using PanoKimliklendirme.Domain.Models;
using PanoKimliklendirme.Infrastructure.Repositories.Core;

namespace PanoKimliklendirme.Infrastructure.Repositories
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
