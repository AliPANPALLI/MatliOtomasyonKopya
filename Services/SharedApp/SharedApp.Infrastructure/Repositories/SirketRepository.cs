using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Domain.Models.ReportModels;
using SharedApp.Infrastructure.Repositories.Core;

namespace SharedApp.Infrastructure.Repositories
{
    public class SirketRepository : Repository<Sirket>, ISirketRepository
    {
        private readonly SharedDbContext _context;
        public SirketRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<int>> GetLokasyonIdsBySirketIds(List<int> sirketIds)
        {
            var sirketLokasyonlar = await _context.Lokasyon.AsNoTracking().Where(x => sirketIds.Contains(x.SirketId) && x.Silindi != true).Select(x => x.Id).ToListAsync();
            return sirketLokasyonlar;
        }
        public async Task<List<Tesis>> GetTesislerByLokasyonId(int lokasyonId)
        {
            List<Tesis> tesisler = new List<Tesis>();
            tesisler = await _context.Tesis.Where(x => x.LokasyonId == lokasyonId).ToListAsync();
            return tesisler;
        }
        public async Task<List<Bolum>> GetBolumlerByTesisId(int tesisId)
        {
            List<Bolum> bolumler = new List<Bolum>();
            bolumler = await _context.Bolum.Where(x => x.TesisId == tesisId).ToListAsync();
            return bolumler;
        }

        public async Task<List<Tbl_Makine>> GetMakinelerByBolumId(int bolumId)
        {
            List<Tbl_Makine> makineler = new List<Tbl_Makine>();
            var sql = @"
        SELECT m.*
        FROM OT_NesneIzleme.dbo.Tbl_Makine AS m WITH (NOLOCK)
        INNER JOIN OTM_Shared.dbo.Bolum AS b WITH (NOLOCK) ON b.Id = m.Bolum_Id
        WHERE b.Id = {0};
    ";

            makineler = await _context.Set<Tbl_Makine>()
                .FromSqlRaw(sql, bolumId)
                .AsNoTracking()
                .ToListAsync();
            return makineler;
        }

        public async Task<List<Tbl_Nesne>> GetNesnelerByMakineId(int makineId)
        {
            List<Tbl_Nesne> nesneler = new();
            var sql = @"
        SELECT n.*
        FROM OT_NesneIzleme.dbo.Tbl_Nesne AS n WITH (NOLOCK)
        INNER JOIN OT_NesneIzleme.dbo.Tbl_Makine AS m WITH (NOLOCK) ON m.Id = n.Makine_Id
        WHERE m.Id = {0};
    ";

            nesneler = await _context.Set<Tbl_Nesne>()
                .FromSqlRaw(sql, makineId)
                .AsNoTracking()
                .ToListAsync();
            return nesneler;
        }

        public async Task AddLokasyon(Lokasyon lokasyon)
        {
            await _context.Lokasyon.AddAsync(lokasyon);
            await _context.SaveChangesAsync();
        }

        public async Task AddTesis(Tesis tesis)
        {
            await _context.Tesis.AddAsync(tesis);
            await _context.SaveChangesAsync();
        }

        public async Task AddBolum(Bolum bolum)
        {
            await _context.Bolum.AddAsync(bolum);
            await _context.SaveChangesAsync();
        }

        public async Task AddMakine(Tbl_Makine tbl_Makine, CancellationToken ct = default)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
        INSERT INTO OT_NesneIzleme.dbo.Tbl_Makine (Name, Bolum_Id, Aciklama)
        VALUES ({tbl_Makine.Name}, {tbl_Makine.Bolum_Id}, {tbl_Makine.Aciklama});
    ", ct);
        }

        public async Task AddNesne(Tbl_Nesne tbl_Nesne, CancellationToken ct = default)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
        INSERT INTO OT_NesneIzleme.dbo.Tbl_Nesne (Name, Makine_Id, Aciklama)
        VALUES ({tbl_Nesne.Name}, {tbl_Nesne.Makine_Id}, {tbl_Nesne.Aciklama});
    ", ct);
        }

    }
}
