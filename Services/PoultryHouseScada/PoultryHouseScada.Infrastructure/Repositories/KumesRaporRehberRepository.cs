using PoultryHouseScada.Domain.Interfaces;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure.Repositories.Core;
using Microsoft.EntityFrameworkCore;

namespace PoultryHouseScada.Infrastructure.Repositories
{
    public class KumesRaporRehberRepository : Repository<KumesRaporRehber>, IKumesRaporRehberRepository
    {
        private readonly KumesScadaDbContext _context;
        public KumesRaporRehberRepository(KumesScadaDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<List<KumesRaporRehber>> GetByLokasyonId(int lokasyonId)
        {
            return await _context.KumesRaporRehber.Where(x => x.LokasyonId == lokasyonId && x.Silindi != true).OrderBy(x => x.RehberAdi).ThenBy(x => x.HaftaSayisi).ToListAsync();
        }

        public async Task<List<KumesRaporRehber>> GetKumesRaporRehberByLokasyonIdAndGuideTipi(int lokasyonId, string guideTipi, int hayvanCinsId)
        {
            return await _context.KumesRaporRehber.Where(x => x.LokasyonId == lokasyonId && x.RehberAdi == guideTipi && x.HayvanCinsId == hayvanCinsId && x.Silindi != true).OrderBy(x => x.HaftaSayisi).ToListAsync();
        }
    }
}