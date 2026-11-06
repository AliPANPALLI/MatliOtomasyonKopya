using KumesScada.Domain.Interfaces;
using KumesScada.Domain.Models;
using KumesScada.Infrastructure.Repositories.Core;
using Microsoft.EntityFrameworkCore;

namespace KumesScada.Infrastructure.Repositories
{
    public class KumesLogRepository : Repository<KumesLogYeni>, IKumesLogRepository
    {
        private readonly KumesScadaDbContext _context;
        public KumesLogRepository(KumesScadaDbContext context) : base(context)
        {
            _context = context;
        }

        //public async Task<List<KumesLog?>?> GetLastKumesLogs()
        //{
        //    var sonKayitlar = await _context.KumesLog.Where(x => x.KayitZamani >= DateTime.Today && x.KayitZamani < DateTime.Today.AddDays(1)).GroupBy(x => x.KumesId).Select(g => g.OrderByDescending(x => x.KayitZamani).FirstOrDefault()).ToListAsync();
        //    return sonKayitlar;
        //}

        public async Task<List<KumesLogYeni?>?> GetLastKumesLogs()
        {
            var sonKayitlar = await _context.KumesLogYeni.Where(x => x.KayitZamani >= DateTime.Today && x.KayitZamani < DateTime.Today.AddDays(1)).GroupBy(x => x.KumesId).Select(g => g.OrderByDescending(x => x.KayitZamani).FirstOrDefault()).ToListAsync();
            return sonKayitlar;
        }

        //public async Task<List<KumesLog?>?> GetLastTenKumesLogs()
        //{
        //    var start = DateTime.Today.AddDays(-9); // 10 gün: -9..0
        //    var end = DateTime.Today.AddDays(1);  // yarın 00:00 (exclusive)

        //    var sql = @"
        //SELECT *
        //FROM (
        //    SELECT kl.*,
        //           ROW_NUMBER() OVER (
        //               PARTITION BY kl.KumesId, CAST(kl.KayitZamani AS date)
        //               ORDER BY kl.KayitZamani DESC
        //           ) AS rn
        //    FROM dbo.KumesLog kl
        //    WHERE kl.KayitZamani >= {0} AND kl.KayitZamani < {1}
        //) t
        //WHERE t.rn = 1
        //ORDER BY t.KumesId, t.KayitZamani;";

        //    return await _context.KumesLog.FromSqlRaw(sql, start, end).AsNoTracking().ToListAsync();
        //}

        public async Task<List<KumesLogYeni?>?> GetLastTenKumesLogs()
        {
            var start = DateTime.Today.AddDays(-9); // 10 gün: -9..0
            var end = DateTime.Today.AddDays(1);  // yarın 00:00 (exclusive)

            var sql = @"
        SELECT *
        FROM (
            SELECT kl.*,
                   ROW_NUMBER() OVER (
                       PARTITION BY kl.KumesId, CAST(kl.KayitZamani AS date)
                       ORDER BY kl.KayitZamani DESC
                   ) AS rn
            FROM dbo.KumesLogYeni kl
            WHERE kl.KayitZamani >= {0} AND kl.KayitZamani < {1}
        ) t
        WHERE t.rn = 1
        ORDER BY t.KumesId, t.KayitZamani;";

            return await _context.KumesLogYeni.FromSqlRaw(sql, start, end).AsNoTracking().ToListAsync();
        }

        //public async Task<List<KumesLog?>?> GetLastKumesLogsByKumesIds(List<int> kumesIds)
        //{
        //    var sonKayitlar = await _context.KumesLog.Where(x => kumesIds.Contains(x.KumesId.GetValueOrDefault(0)) && x.KayitZamani >= DateTime.Today && x.KayitZamani < DateTime.Today.AddDays(1)).GroupBy(x => x.KumesId).Select(g => g.OrderByDescending(x => x.KayitZamani).FirstOrDefault()).ToListAsync();
        //    return sonKayitlar;
        //}

        public async Task<List<KumesLogYeni?>?> GetLastKumesLogsByKumesIds(List<int> kumesIds)
        {
            var sonKayitlar = await _context.KumesLogYeni.Where(x => kumesIds.Contains(x.KumesId.GetValueOrDefault(0)) && x.KayitZamani >= DateTime.Today && x.KayitZamani < DateTime.Today.AddDays(1)).GroupBy(x => x.KumesId).Select(g => g.OrderByDescending(x => x.KayitZamani).FirstOrDefault()).ToListAsync();
            return sonKayitlar;
        }
    }
}