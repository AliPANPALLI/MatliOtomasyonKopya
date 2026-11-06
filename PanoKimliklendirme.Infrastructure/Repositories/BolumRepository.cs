using Microsoft.EntityFrameworkCore;
using PanoKimliklendirme.Domain.Interfaces;
using PanoKimliklendirme.Domain.Models;
using PanoKimliklendirme.Infrastructure.Repositories.Core;

namespace PanoKimliklendirme.Infrastructure.Repositories
{
    public class BolumRepository : Repository<Bolum>, IBolumRepository
    {
        private readonly PanoKimliklendirmeDbContext _context;
        public BolumRepository(PanoKimliklendirmeDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<int> FindLokasyonIdByBolumId(int bolumId)
        {
            var bolum = await GetRootBolumIdByBolumId(bolumId);
            return bolum.LokasyonId;
        }

        public async Task<Bolum> GetRootBolumIdByBolumId(int bolumId)
        {
            //Bölüm zincirinin en üstüne çık
            var bolum = await _context.Bolum.FirstOrDefaultAsync(b => b.Id == bolumId && b.Silindi != true);
            while (bolum != null && bolum.ParentBolumId > 0)
            {
                bolum = await _context.Bolum.FirstOrDefaultAsync(b => b.Id == bolum.ParentBolumId && b.Silindi != true);
            }

            if (bolum == null)
                throw new Exception("Bölüm bulunamadı");
            //En üst bölümü döndür
            return bolum;
        }
    }
}
