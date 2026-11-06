using Microsoft.EntityFrameworkCore;
using PanoKimliklendirme.Domain.Interfaces;
using PanoKimliklendirme.Domain.Models;
using PanoKimliklendirme.Infrastructure.Repositories.Core;

namespace PanoKimliklendirme.Infrastructure.Repositories
{
    public class MakineRepository : Repository<Makine>, IMakineRepository
    {
        private readonly PanoKimliklendirmeDbContext _context;
        public MakineRepository(PanoKimliklendirmeDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<int> FindLokasyonIdByMakineId(int makineId)
        {
            var bolum = await GetRootBolumIdByMakineId(makineId);
            return bolum.LokasyonId;
        }
        public async Task<Bolum> GetRootBolumIdByMakineId(int makineId)
        {
            var makine = await _context.Makine.FirstOrDefaultAsync(m => m.Id == makineId && m.Silindi != true);
            while (makine != null && makine.ParentMakineId > 0)
            {
                makine = await _context.Makine.FirstOrDefaultAsync(m => m.Id == makine.ParentMakineId && m.Silindi != true);
            }

            //Makine en üst makineye ulaştıysa, bir bölüme bağlı mı kontrol et
            int? bolumId = makine?.BolumId;

            //Bölüm zincirinin en üstüne çık
            var bolum = await _context.Bolum.FirstOrDefaultAsync(b => b.Id == bolumId && b.Silindi != true);
            while (bolum != null && bolum.ParentBolumId > 0)
            {
                bolum = await _context.Bolum.FirstOrDefaultAsync(b => b.Id == bolum.ParentBolumId && b.Silindi != true);
            }

            if (bolum == null)
                throw new Exception("Bölüm bulunamadı");
            //En üst bölümü TreeModel olarak döndür
            return bolum;
        }
    }
}
