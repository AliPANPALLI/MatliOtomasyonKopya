using Microsoft.EntityFrameworkCore;
using PanelIdentification.Domain.Interfaces;
using PanelIdentification.Domain.Models;
using PanelIdentification.Infrastructure.Repositories.Core;
using static SharedKernel.ApplicationConstants;

namespace PanelIdentification.Infrastructure.Repositories
{
    public class CihazRepository : Repository<Cihaz>, ICihazRepository
    {
        private readonly PanoKimliklendirmeDbContext _context;
        public CihazRepository(PanoKimliklendirmeDbContext context) : base(context)
        {
            _context = context;
        }
        
        public async Task<int> FindLokasyonIdByCihazId(int cihazId)
        {
            var bolum = await GetRootBolumIdByCihazId(cihazId);
            return bolum.LokasyonId;
        }

        public async Task<Bolum> GetRootBolumIdByCihazId(int cihazId)
        {
            var cihaz = await _context.Cihaz.FirstOrDefaultAsync(x => x.Id == cihazId);
            // 1. Pano zincirinin en üstüne çık
            var pano = await _context.Pano.FirstOrDefaultAsync(p => p.Id == cihaz.PanoId && p.Silindi != true);
            while (pano != null && pano.ParentPanoId > 0)
            {
                pano = await _context.Pano.FirstOrDefaultAsync(p => p.Id == pano.ParentPanoId && p.Silindi != true);
            }

            // 2. Eğer en üst pano bulunduysa, bir makineye bağlı mı kontrol et
            var panoMakineEsleme = await _context.PanoMakineEsleme
                .FirstOrDefaultAsync(pme => pme.PanoId == pano.Id && pme.Silindi != true);

            int? makineId = panoMakineEsleme?.MakineId;

            // 3. Makine zincirinin en üstüne çık
            var makine = await _context.Makine.FirstOrDefaultAsync(m => m.Id == makineId && m.Silindi != true);
            while (makine != null && makine.ParentMakineId > 0)
            {
                makine = await _context.Makine.FirstOrDefaultAsync(m => m.Id == makine.ParentMakineId && m.Silindi != true);
            }

            // 4. Makine en üst makineye ulaştıysa, bir bölüme bağlı mı kontrol et
            int? bolumId = makine?.BolumId;

            // 5. Bölüm zincirinin en üstüne çık
            var bolum = await _context.Bolum.FirstOrDefaultAsync(b => b.Id == bolumId && b.Silindi != true);
            while (bolum != null && bolum.ParentBolumId > 0)
            {
                bolum = await _context.Bolum.FirstOrDefaultAsync(b => b.Id == bolum.ParentBolumId && b.Silindi != true);
            }

            if (bolum == null)
                throw new Exception("Bölüm bulunamadı");
            // 6. En üst bölümü TreeModel olarak döndür
            return bolum;
        }
    }
}
