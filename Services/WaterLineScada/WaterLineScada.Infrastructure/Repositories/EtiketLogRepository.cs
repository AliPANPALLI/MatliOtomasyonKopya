using Microsoft.EntityFrameworkCore;
using WaterLineScada.Domain.Interfaces;
using WaterLineScada.Domain.Models;
using WaterLineScada.Infrastructure.Repositories.Core;

namespace WaterLineScada.Infrastructure.Repositories
{
    public class EtiketLogRepository : Repository<EtiketLogYeni>, IEtiketLogRepository
    {
        private readonly SuHatScadaDbContext _context;
        public EtiketLogRepository(SuHatScadaDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<List<EtiketLogYeni>> GetEtiketLogGunSonuVeriler(IReadOnlyCollection<int> etiketIds)
        {
            var ids = etiketIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new List<EtiketLogYeni>();
            }

            var yesterday = DateTime.Today.AddDays(-1);
            var today = DateTime.Today;

            return await _context.EtiketLogYeni
                .AsNoTracking()
                .Where(x => x.EtiketId.HasValue &&
                            ids.Contains(x.EtiketId.Value) &&
                            x.KayitZamani >= yesterday &&
                            x.KayitZamani < today)
                .GroupBy(x => x.EtiketId)
                .Select(group => group
                    .OrderByDescending(x => x.KayitZamani)
                    .ThenByDescending(x => x.Id)
                    .First())
                .ToListAsync();
        }
    }
}
