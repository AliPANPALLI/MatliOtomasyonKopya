using Microsoft.EntityFrameworkCore;
using PanoKimliklendirme.Domain.Interfaces;
using PanoKimliklendirme.Domain.Models;
using PanoKimliklendirme.Infrastructure.Repositories.Core;
using static SharedKernel.ApplicationConstants;

namespace PanoKimliklendirme.Infrastructure.Repositories
{
    public class PanoRepository : Repository<Pano>, IPanoRepository
    {
        private readonly PanoKimliklendirmeDbContext _context;
        public PanoRepository(PanoKimliklendirmeDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<int> FindLokasyonIdByPanoId(int panoId)
        {
            var bolum = await GetRootBolumIdByPanoId(panoId);
            return bolum.LokasyonId;
        }

        public async Task<Bolum> GetRootBolumIdByPanoId(int panoId)
        {
            // 1. Pano zincirinin en üstüne çık
            var pano = await _context.Pano.FirstOrDefaultAsync(p => p.Id == panoId && p.Silindi != true);
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

        public async Task<List<Tuple<string, int, string>>> GetParentNodesByPanoIdForDosyaYolu(int panoId)
        {
            List<Tuple<string, int, string>> result = new List<Tuple<string, int, string>>();

            // 1. Pano zincirinin en üstüne çık
            var pano = await _context.Pano.FirstOrDefaultAsync(p => p.Id == panoId && p.Silindi != true);
            result.Add(new Tuple<string, int, string>(pano.Name, pano.Id, TreeViewNodeTypes.Pano));
            while (pano != null && pano.ParentPanoId > 0)
            {
                pano = await _context.Pano.FirstOrDefaultAsync(p => p.Id == pano.ParentPanoId && p.Silindi != true);
                result.Add(new Tuple<string, int, string>(pano.Name, pano.Id, TreeViewNodeTypes.Pano));
            }

            // 2. Eğer en üst pano bulunduysa, bir makineye bağlı mı kontrol et
            var panoMakineEsleme = await _context.PanoMakineEsleme
                .FirstOrDefaultAsync(pme => pme.PanoId == pano.Id && pme.Silindi != true);

            int? makineId = panoMakineEsleme?.MakineId;

            // 3. Makine zincirinin en üstüne çık
            var makine = await _context.Makine.FirstOrDefaultAsync(m => m.Id == makineId && m.Silindi != true);
            result.Add(new Tuple<string, int, string>(makine.Name, makine.Id, TreeViewNodeTypes.Makine));
            while (makine != null && makine.ParentMakineId > 0)
            {
                makine = await _context.Makine.FirstOrDefaultAsync(m => m.Id == makine.ParentMakineId && m.Silindi != true);
                result.Add(new Tuple<string, int, string>(makine.Name, makine.Id, TreeViewNodeTypes.Makine));
            }

            // 4. Makine en üst makineye ulaştıysa, bir bölüme bağlı mı kontrol et
            int? bolumId = makine?.BolumId;

            // 5. Bölüm zincirinin en üstüne çık
            var bolum = await _context.Bolum.FirstOrDefaultAsync(b => b.Id == bolumId && b.Silindi != true);
            result.Add(new Tuple<string, int, string>(bolum.Name, bolum.Id, TreeViewNodeTypes.Bolum));
            while (bolum != null && bolum.ParentBolumId > 0)
            {
                bolum = await _context.Bolum.FirstOrDefaultAsync(b => b.Id == bolum.ParentBolumId && b.Silindi != true);
                result.Add(new Tuple<string, int, string>(bolum.Name, bolum.Id, TreeViewNodeTypes.Bolum));
            }

            if (bolum == null)
                throw new Exception("Bölüm bulunamadı");

            return result;
        }
    }
}